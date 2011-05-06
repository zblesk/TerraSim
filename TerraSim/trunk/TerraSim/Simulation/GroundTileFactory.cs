using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TerraSim.Simulation
{
    public class GroundTileFactory
    {
        public delegate object DispatchGenerator(GroundTile tile);

        private int tilesCreated = 0;
        private GroundTile[] tiles = null;
        //private Stream stream = null;

        //names of the stored variables:
        private string speedcap = "speedcap";
        private string light = "lighting_reduction";
        private string tile_type = "type";

        /// <summary>
        /// Called to get a dispatcher for each new GroundTile instance.
        /// </summary>
        public DispatchGenerator DispatcherGenerator;

        /// <summary>
        /// Load tile definitions from a stream.
        /// </summary>
        /// <param name="tileData">The input stream.</param>
        /// <remarks>Will not dispose the input stream.</remarks>
        public GroundTileFactory(Stream tileData)
        {
            StreamReader input = new StreamReader(tileData);
            var loaded = JsonConvert.DeserializeObject<JObject[]>(input.ReadToEnd());
            var res = from obj in loaded
                      select new GroundTile("", (int)obj[speedcap],
                    (IntensityLevel)(int)obj[light], (string)obj[tile_type]);
            tiles = res.ToArray();
        }

        public GroundTile CreateTile(string typeId)
        {
            var tile = from t in tiles
                       where t.Type == typeId
                       select CloneNewName(t, "{0}_{1}".Form(typeId, ++tilesCreated), typeId);
            var @enum = tile.GetEnumerator();
            if (!@enum.MoveNext())
            {
                return null;
            }
            if (DispatcherGenerator != null)
            {
                @enum.Current.DispatchMediator = DispatcherGenerator(@enum.Current);
            }
            return @enum.Current;
        }

        private GroundTile CloneNewName(GroundTile tile, string newName, string type)
        {
            return new GroundTile(newName, tile.SpeedCap, tile.Lighting, type);
        }
    }
}