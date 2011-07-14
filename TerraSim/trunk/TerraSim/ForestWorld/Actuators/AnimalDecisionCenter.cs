using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CSharp.RuntimeBinder;
using TerraSim.ForestWorld.Entities;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Actuators
{
    internal class DecisionCenter : Actuator
    {
        private static string[] actions = new string[0];
        private AnimalFeeder target;
        private int timeRemaining = 0;
        private const int eatingInterval = 1;
        private int timeSinceLastFood = 0;
        private bool eating = false, fleeing = false;
        private new Animal Owner { get { return (Animal)base.Owner; } }

        public DecisionCenter(Agent owner, ServiceMediator mediator)
            : base(owner, mediator) { }

        public override string[] ActionList()
        {
            return actions;
        }

        public override void Perform(WorldUpdateEventArgs args,
            string action, string argument1, string argument2) { }

        public override void Update(WorldUpdateEventArgs args)
        {
            timeRemaining = (timeRemaining - 1).Bound(-1, int.MaxValue);
            if (timeRemaining > 0)
            {
                return;
            }
            if (timeRemaining == 0)
            {
                if (eating)
                {
                    Eat(target);
                    target = null;
                    eating = false;
                }
                return;
            }
            // Check when was the last time the animal has eaten and if it's been too long, 
            // try to find some food.
            var r = new Random();
            timeSinceLastFood++;
            var fed = false;
            var wantsFood = timeSinceLastFood > eatingInterval;
            if (wantsFood)
            {
                var surroundings = from hex in args.World.Map.EnumerateNeighbours(
                                       Owner.PositionX, Owner.PositionY, Animal.SightRange)
                                   from obj in hex
                                   where obj.Type == AnimalFeeder.TypeString
                                   select obj;
                var feeders = surroundings.ToArray();
                if (feeders.Length > 0) // feeders found
                {
                    target = (AnimalFeeder)feeders[r.Next(feeders.Length)];
                    if (args.World.Map.Distance(Owner.PositionX, Owner.PositionY,
                        target.PositionX, target.PositionY) <= 1)
                    {
                        eating = true;
                        timeRemaining = 1;
                        fed = true;
                    }
                    else
                    {
                        var dx = target.PositionX - Owner.PositionX;
                        var dy = target.PositionY - Owner.PositionY;
                        Mediator.ExecuteAfterUpdate(() =>
                        {
                            args.World.MoveObject(Owner,
                                Owner.PositionX + dx / (Math.Abs(dx) == 0 ? 1 : Math.Abs(dx)),
                                Owner.PositionY + dy / (Math.Abs(dy) == 0 ? 1 : Math.Abs(dy)));
                        });
                    }
                }
            }
            if (!wantsFood || !fed) //not feeding; move in a random direction.
            {
                var neighb = args.World.Map.EnumerateNeighbours(Owner.PositionX,
                    Owner.PositionY, 1).ToArray();
                var next = neighb[r.Next(neighb.Length)].Objects[0];
                Mediator.ExecuteAfterUpdate(() =>
                {
                    args.World.MoveObject(Owner, next.PositionX, next.PositionY);
                });
            }
        }

        public override void Marshal(DataCollection dataCollection)
        {
            if (eating)
            {
                dataCollection.AddAction(Owner.Name, "eating", target.Name, "");
            }
        }

        private void Eat(AnimalFeeder feeder)
        {
            try
            {
                var items = (HashSet<NamedObject>)(((dynamic)(feeder.DispatchMediator))
                    .TakeAll());
                var poisonous = from item in items
                                let p = item as Plant
                                where (p != null) && p.IsPoisonous
                                select p;
                timeSinceLastFood = 0;
                if (poisonous.GetEnumerator().MoveNext())
                {
                    // It has eaten something poisonous, so it dies.
                    Mediator.ExecuteAfterUpdate(() => { Owner.Die(); });
                }
            }
            catch (RuntimeBinderException)
            {
                Trace.TraceError(
                    "The animal feeder {0} didn't react to message."
                    .Form(feeder.Name));
                throw;
            }
        }

    }
}
