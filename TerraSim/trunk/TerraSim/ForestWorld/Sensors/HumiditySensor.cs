using System.Linq;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Sensors
{
    public sealed class HumiditySensor : Sensor
    {
        private const int sightRange = 3;

        public HumiditySensor(UserAgent owner) : base(owner) { }

        public override void Sense(WorldUpdateEventArgs args)
        {
            LastInput.Clear();
            var seen = args.World.Map.EnumerateNeighbours(
                Owner.PositionX, Owner.PositionY, sightRange);
            var tiles = from hex in seen
                        from item in hex
                        where item is GroundTile
                        select item;
            foreach (GroundTile tile in tiles)
            {
                LastInput.AddAttribute(tile.Name, "position_x", tile.PositionX.ToString());
                LastInput.AddAttribute(tile.Name, "position_y", tile.PositionY.ToString());
                LastInput.AddAttribute(tile.Name, "humidity", tile.Humidity.ToString());
            }
        }
    }
}
