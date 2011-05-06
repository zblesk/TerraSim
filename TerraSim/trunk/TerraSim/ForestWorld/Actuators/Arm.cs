using System;
using Microsoft.CSharp.RuntimeBinder;
using TerraSim.ForestWorld.Entities;
using TerraSim.ForestWorld.Sensors;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Actuators
{
    /// <summary>
    /// Class representing the robot's arm. It can only hold plants.
    /// </summary>
    public class Arm : Actuator
    {
        private static string[] actions = { "take", "put_on_position", "put_in_item" };
        private const int actionPrice = 6;
        /// <summary>
        /// The action currently being executed. It is an index to the 'actions' array.
        /// </summary>
        private int currentAction = -1;
        private int timeRemaining = 0;
        private NamedObject target = null;
        private int targetX, targetY;
        private Plant heldItem = null;

        public Arm(Agent owner, ServiceMediator mediator) : base(owner, mediator) { }

        public override string[] ActionList()
        {
            return actions;
        }

        public override void Perform(WorldUpdateEventArgs args, string action, string argument1, string argument2)
        {
            switch (action)
            {
                case ("take"):
                    Plant tar = args.World.GetObjectByName(argument1) as Plant;
                    if ((tar == null)
                        || (heldItem != null)
                        || (args.World.Map.Distance(Owner.PositionX, Owner.PositionY,
                            tar.PositionX, tar.PositionY) > 1)
                        || !HasEnoughEnergy())
                    {
                        break;
                    }
                    target = tar;
                    currentAction = 0;
                    timeRemaining = 1;
                    break;
                case ("put_on_position"):
                    if ((heldItem == null)
                        || (!int.TryParse(argument1.Trim(), out targetX))
                        || (!int.TryParse(argument2.Trim(), out targetY))
                        || (args.World.Map.Distance(Owner.PositionX, Owner.PositionY,
                            targetX, targetY) > 1)
                        || !HasEnoughEnergy())
                    {
                        break;
                    }
                    currentAction = 1;
                    timeRemaining = 1;
                    break;
                case ("put_in_item"):
                    target = args.World.GetObjectByName(argument1);
                    if ((heldItem == null)
                        || (target == null)
                        || (args.World.Map.Distance(Owner.PositionX, Owner.PositionY,
                            targetX, targetY) > 1)
                        || !HasEnoughEnergy())
                    {
                        break;
                    }
                    currentAction = 2;
                    timeRemaining = 1;
                    break;                    
                default:
                    throw new Exception("Unknown action name.");
            }
        }

        public override void Update(WorldUpdateEventArgs args)
        {
            if (currentAction >= 0)
            {
                if (timeRemaining == 0)
                {
                    PerformAction(args.World);
                    currentAction = -1;
                    target = null;
                }
                else
                {
                    timeRemaining--;
                }
            }
        }

        public override void Marshal(DataCollection dataCollection)
        {
            if (heldItem != null)
            {
                dataCollection.AddAttribute(Owner.Name, "holding", heldItem.Name);
            }
            if ((currentAction >= 0) && (timeRemaining >= 0))
            {
                if (currentAction == 1)
                {
                    dataCollection.AddAction(Owner.Name, actions[currentAction],
                        targetX.ToString(), targetY.ToString());
                }
                else
                {
                    dataCollection.AddAction(Owner.Name, actions[currentAction], target.Name, "");
                }
            }
        }

        private void PerformAction(World world)
        {
            switch (currentAction)
            {
                case (0): //take
                    world.RemoveObject(target);
                    target.PositionY = target.PositionX = -1;
                    heldItem = (Plant)target;
                    try
                    {
                        ((dynamic)(heldItem.DispatchMediator)).WasPickedUp();
                    }
                    catch (RuntimeBinderException) { }
                    break;
                case (1): //put on
                    world.AddObject(heldItem, targetX, targetY);
                    heldItem = null;
                    break;
                case (2): //put into
                    world.AddObject(heldItem, target.PositionX, target.PositionY);
                    try
                    {
                        ((dynamic)(target.DispatchMediator)).Put(heldItem);
                    }
                    catch (RuntimeBinderException) { }
                    heldItem = null;
                    break;
            }
        }

        private bool HasEnoughEnergy()
        {
            bool result = false;
            Mediator.MessageBroadcaster.Broadcast(
                Accumulator.EnergyPaymentCommand,
                actionPrice,
                Owner,
                (b) => { result = (bool)b; },
                Owner.Name);
            return result;
        }
    }
}
