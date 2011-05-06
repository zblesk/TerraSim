using System;
using Microsoft.CSharp.RuntimeBinder;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Actuators
{
    public class PlantAnalyzer : Actuator
    {
        private NamedObject target = null;
        private int timeRemaining = -1;
        private int timeRequired = 5;
        private int energyRequired = 4;

        public PlantAnalyzer(Agent owner, ServiceMediator mediator)
            : base(owner, mediator)
        {
        }

        private static string[] actions = { "analyze" };
        
        public override string[] ActionList()
        {
            return actions;
        }

        public override void Perform(WorldUpdateEventArgs args, string action, string argument1, string argument2)
        {
            switch (action)
            {
                case ("analyze"):
                    target = args.World.GetObjectByName(argument1);
                    if (target != null)
                    {
                        TryAnalyze(target);
                    }
                    break;
                default:
                    throw new Exception("Unknown action name.");
            }
        }

        private void TryAnalyze(NamedObject argument)
        {
            //check if we have enough energy
            bool hasEnoughEnergy = false;
            Mediator.MessageBroadcaster.Broadcast(
                TerraSim.ForestWorld.Sensors.Accumulator.EnergyPaymentCommand,
                energyRequired,
                Owner,
                (b) => { hasEnoughEnergy = (bool)b; },
                Owner.Name);
            if (!hasEnoughEnergy)
            {
                return;
            }
            timeRemaining = timeRequired;
        }

        public override void Update(WorldUpdateEventArgs args)
        {
            if (timeRemaining < 0)
            {
                return;
            }
            if (timeRemaining == 0)
            {
                PerformAction(args.World);
                timeRemaining = -1;
            }
            else
            {
                timeRemaining--;
            }
        }

        public override void Marshal(DataCollection dataCollection)
        {
            if (timeRemaining >= 0)
            {
                dataCollection.AddAction(Owner.Name, "analyzing", target.Name, "");
            }
        }

        private void PerformAction(World world)
        {
            try
            {
                ((dynamic)(target.DispatchMediator)).WasAnalyzed();
            }
            catch (RuntimeBinderException)
            {
                //the object didn't understand the message.
                //default behaviour is to do nothing.
            }
        }
    }
}
