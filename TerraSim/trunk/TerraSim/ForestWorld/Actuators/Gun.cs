using System;
using Microsoft.CSharp.RuntimeBinder;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Actuators
{
    /// <summary>
    /// An actuator allowing the agent to shoot at objects.
    /// </summary>
    public sealed class Gun : Actuator
    {
        internal static string LoudNoiseAction = "loud_noise";
        private static readonly string[] actions = new string[] { "gun_stats", "shoot", "reload" };
        /// <summary>
        /// The action currently being executed. It is an index to the 'actions' array.
        /// </summary>
        private int currentAction = -1;
        private int timeRemaining = 0;
        private const int magazineCapacity = 3;
        private int bulletsLeft = magazineCapacity;
        private string target = "";
        private int actionPrice = 3;

        #region Stats
        private bool sendStats = false;
        private int bulletsShot = 0;
        private int reloadCount = 0;
        #endregion

        public Gun(UserAgent owner, ServiceMediator sm) : base(owner, sm) { }

        public override string[] ActionList()
        {
            return actions;
        }

        public override void Perform(WorldUpdateEventArgs args, string action,
            string argument1, string argument2)
        {
            switch (action)
            {
                case ("gun_stats"):
                    sendStats = true;
                    break;
                case ("shoot"):
                    if ((bulletsLeft < 1) || !HasEnoughEnergy())
                    {
                        break;
                    }
                    currentAction = 1;
                    timeRemaining = 1;
                    target = argument1;
                    break;
                case ("reload"):
                    if (!HasEnoughEnergy())
                    {
                        break;
                    }
                    currentAction = 2;
                    timeRemaining = 2;
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
                    PerformAction(args);
                    currentAction = -1;
                }
                else
                {
                    timeRemaining--;
                }
            }
        }

        public override void Marshal(DataCollection dataCollection)
        {
            dataCollection.AddAttribute(Owner.Name, "bullets_left", bulletsLeft.ToString());
            if (sendStats)
            {
                dataCollection.AddAttribute(Owner.Name, "magazine_capacity", 
                    magazineCapacity.ToString());
                dataCollection.AddAttribute(Owner.Name, "bullets_shot", bulletsShot.ToString());
                dataCollection.AddAttribute(Owner.Name, "reload_count", reloadCount.ToString());
                sendStats = false;
            }
            if ((currentAction >= 0) && (timeRemaining >= 0))
            {
                dataCollection.AddAction(Owner.Name, actions[currentAction], target, "");
            }
        }

        private void PerformAction(WorldUpdateEventArgs args)
        {
            switch (currentAction)
            {
                case (1):
                    bulletsShot++;
                    bulletsLeft--;
                    var obj = args.World.GetObjectByName(target);
                    if (obj == null)
                    {
                        break;
                    }
                    try
                    {
                        ((dynamic)(obj.DispatchMediator)).WasShot();
                    }
                    catch (RuntimeBinderException)
                    {
                        //the object didn't know how to get shot; 
                        //default behaviour is to do nothing.
                    }
                    Mediator.MessageBroadcaster.Broadcast(LoudNoiseAction,
                        null, Owner);
                    break;
                case(2):
                    bulletsLeft = magazineCapacity;
                    reloadCount++;
                    break;
                default:
                    throw new Exception("Internal inconsistency encountered - unknown action.");
            }
        }

        private bool HasEnoughEnergy()
        {
            bool result = false;
            Mediator.MessageBroadcaster.Broadcast(
                TerraSim.ForestWorld.Sensors.Accumulator.EnergyPaymentCommand,
                actionPrice,
                Owner,
                (b) => { result = (bool)b; },
                Owner.Name);
            return result;
        }
    }
}
