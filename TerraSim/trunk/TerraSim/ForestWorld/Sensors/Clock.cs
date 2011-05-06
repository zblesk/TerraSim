using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Sensors
{
    /// <summary>
    /// A sensor informing the agent about the current time.
    /// </summary>
    public sealed class Clock : Sensor
    {
        public Clock(UserAgent owner) : base(owner) { } 

        public override void Sense(WorldUpdateEventArgs args)
        {
            LastInput.Clear();
            LastInput.AddAttribute("world", "time", args.World.TimeOfDay.ToString());
        }
    }
}
