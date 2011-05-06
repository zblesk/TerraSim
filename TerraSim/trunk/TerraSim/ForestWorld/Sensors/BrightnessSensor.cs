using System.Linq;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Sensors
{
    /// <summary>
    /// A sensor informing the agent about the amount of light shining on him.
    /// </summary>
    public sealed class BrightnessSensor : Sensor
    {
        public BrightnessSensor(UserAgent owner) : base(owner) { }

        public override void Sense(WorldUpdateEventArgs args)
        {
            LastInput.Clear();
            var tiles = (from obj in args.World.Map[Owner.PositionX, Owner.PositionY]
                         where obj is GroundTile
                         select obj).GetEnumerator();
            tiles.MoveNext(); //each hex contains exactly one GroundTile 
                              //- therefore no checks are needed.
            var tile = (GroundTile)tiles.Current;
            LastInput.AddAttribute("world", "light_intensity", tile.Lighting.ToString());
        }
    }
}
