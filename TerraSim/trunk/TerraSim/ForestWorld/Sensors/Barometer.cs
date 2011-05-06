using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Sensors
{
    public class Barometer : Sensor
    {
        public Barometer(UserAgent owner) : base(owner) { } 

        public override void Sense(WorldUpdateEventArgs args)
        {
            LastInput.Clear();
            LastInput.AddAttribute("world", "pressure", args.World.Pressure.ToString());
        }
    }
}
