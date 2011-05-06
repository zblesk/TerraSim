using System;
using Microsoft.CSharp.RuntimeBinder;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Actuators
{
    public class WaterTank : Actuator
    {
        private static string[] actions = { "watertank_stats", "water", "refill" };
        private const string waterTileType = "water";

        private const int maxWaterLevel = 5;
        private int currentWaterLevel = 3;
        /// <summary>
        /// The action currently being executed. It is an index to the 'actions' array.
        /// </summary>
        private int currentAction = -1;
        private int timeRemainig = 0;
        private NamedObject target;
        private int priceOfWatering = 4;

        #region Stats
        private bool sendStats = false;
        private int timesWatered = 0;
        private int timesRefilled = 0;
        #endregion

        public WaterTank(Agent owner, ServiceMediator sm) : base(owner, sm) { }

        public override string[] ActionList()
        {
            return actions;
        }

        public override void Perform(WorldUpdateEventArgs args, string action, string argument1, string argument2)
        {
            switch (action)
            {
                case ("watertank_stats"):
                    sendStats = true;
                    break;
                case ("water"):
                    TryStartWatering(args.World, 
                        args.World.GetObjectByName(argument1));
                    break;
                case ("refill"):
                    TryStartRefilling(args, argument1);
                    break;
                default:
                    throw new Exception("Unknown action name.");
            }
        }

        public override void Update(WorldUpdateEventArgs args)
        {
            if (currentAction >= 0)
            {
                if (timeRemainig == 0)
                {
                    PerformAction(args.World);
                    currentAction = -1;
                    target = null;
                }
                else
                {
                    timeRemainig--;
                }
            }
        }

        public override void Marshal(DataCollection dataCollection)
        {
            if (sendStats)
            {
                sendStats = false;
                dataCollection.AddAttribute(Owner.Name, "times_watered", timesWatered.ToString());
                dataCollection.AddAttribute(Owner.Name, "times_refilled", timesRefilled.ToString());
                dataCollection.AddAttribute(Owner.Name, "tank_capacity", maxWaterLevel.ToString());
            }
            dataCollection.AddAttribute(Owner.Name, "water_level", currentWaterLevel.ToString());
            if ((currentAction >= 0) && (timeRemainig >= 0))
            {
                dataCollection.AddAction(Owner.Name, actions[currentAction], "", "");
            }
        }

        private void PerformAction(World world)
        {
            switch (currentAction)
            {
                case (1):
                    try
                    {
                        ((dynamic)(target.DispatchMediator)).WasWatered();
                    }
                    catch (RuntimeBinderException)
                    {
                        //the object didn't know how to get shot; 
                        //default behaviour is to do nothing.
                    }
                    break;
                case (2):
                    if (world.Map.Distance(target.PositionX, target.PositionY,
                        Owner.PositionX, Owner.PositionY) > 1)
                    {
                        break;
                    }
                    timesRefilled++;
                    currentWaterLevel = maxWaterLevel;
                    break;
                default:
                    throw new Exception("Internal inconsistency encountered - unknown action.");
            }
        }

        private void TryStartWatering(World world, NamedObject newTarget)
        {
            bool @continue = false;
            Mediator.MessageBroadcaster.Broadcast(
                TerraSim.ForestWorld.Sensors.Accumulator.EnergyPaymentCommand,
                priceOfWatering, 
                Owner,
                (b) => { @continue = (bool)b; }, 
                Owner.Name);
            if (!@continue)
            {
                return;
            }
            if ((currentWaterLevel == 0) 
                || (newTarget == null) 
                || world.Map.Distance(newTarget.PositionX, newTarget.PositionY,
                    Owner.PositionX, Owner.PositionY) > 1)
            {
                return;
            }
            target = newTarget;
            currentAction = 1;
            timeRemainig = 1;
            return;
        }

        private void TryStartRefilling(WorldUpdateEventArgs args, string argument)
        {
            timeRemainig = Math.Min((maxWaterLevel - currentWaterLevel) / 2, 1);
            bool @continue = false;
            Mediator.MessageBroadcaster.Broadcast(
                TerraSim.ForestWorld.Sensors.Accumulator.EnergyPaymentCommand,
                timeRemainig * 2, //The more water is supposed to be pumped, the more energy it takes.
                Owner,
                (b) => { @continue = (bool)b; }, 
                Owner.Name);
            target = args.World.GetObjectByName(argument) as GroundTile;
            if (!@continue || (target == null) || (target.Type != waterTileType))
            {
                return;
            }
            currentAction = 2;
        }
    }
}
