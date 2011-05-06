using System;
using System.Linq;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Sensors
{
    /// <summary>
    /// A class representing the battery of the agent.
    /// </summary>
    public class Accumulator : Sensor
    {
        public static string EnergyPaymentCommand = "pay_energy";
        private const int capacity = 70; 
        private int currentLevel = capacity;

        public Accumulator(UserAgent owner, ServiceMediator sm) : base(owner, sm) 
        {
            sm.MessageBroadcaster.SubscribeWithCallback(EnergyPaymentCommand,
                (o, c) => { PayEnergy((int)o, c); }, Owner);
        }

        /// <summary>
        /// Tries to use up some energy. 
        /// </summary>
        /// <param name="amount">Amount of energy to use.</param>
        /// <returns>Returns true if enough energy was available, otherwise false.</returns>
        /// <remarks>If enough energy was available, besides returning true, 
        /// the given amount is subtracted from the battery.</remarks>
        public void PayEnergy(int amount, Action<object> callback)
        {
            if (currentLevel >= amount)
            {
                currentLevel -= amount;
                callback(true);
                return;
            }
            callback(false);
        }

        public override void Sense(WorldUpdateEventArgs args)
        {
            LastInput.Clear();
            var tiles = (from obj in args.World.Map[Owner.PositionX, Owner.PositionY]
                         where obj is GroundTile
                         select obj).GetEnumerator();
            tiles.MoveNext(); //each hex contains exactly one GroundTile 
            //- therefore no checks are needed.
            var tile = (GroundTile)tiles.Current;
            currentLevel += ((int)tile.Lighting); //determines how much energy will be added
            currentLevel = currentLevel.Bound(0, capacity);
            LastInput.AddAttribute(Owner.Name, "battery_energy_level", currentLevel.ToString());
            //An observation about how much energy was charged could be added if needed.
        }
    }
}
