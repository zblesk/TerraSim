using System.Linq;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Sensors
{
    /// <summary>
    /// A sensor letting the agent see around him.
    /// </summary>
    public sealed class Camera : Sensor
    {
        private const int sightRange = 3;

        public Camera(UserAgent owner) : base(owner) { }

        public override void Sense(WorldUpdateEventArgs args)
        {
            LastInput.Clear();
            foreach (var item in args.World.Map[Owner.PositionX, Owner.PositionY])
            {
                if (item != Owner)
                {
                    ProcessItem(item);
                }
            }
            var seen = args.World.Map.EnumerateNeighbours(
                Owner.PositionX, Owner.PositionY, sightRange);
            var tiles = from hex in seen
                        from item in hex
                        select item;
            foreach (var item in tiles)
            {
                ProcessItem(item);
            }
            LastInput.AddAttribute("world", "weather", args.World.CurrentWeather.ToString());
        }

        private GroundTile ProcessItem(NamedObject item)
        {
            GroundTile tile;
            tile = item as GroundTile;
            if (tile != null)
            {
                LastInput.AddAttribute(tile.Name, "position_x", tile.PositionX.ToString());
                LastInput.AddAttribute(tile.Name, "position_y", tile.PositionY.ToString());
                LastInput.AddAttribute(tile.Name, "type", tile.Type);
                LastInput.AddAttribute(tile.Name, "lighting", tile.Lighting.ToString());
            }
            else
            {
                item.Marshal(LastInput);
            }
            return tile;
        }
    }
}
