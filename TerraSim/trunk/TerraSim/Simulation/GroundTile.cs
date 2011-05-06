
using TerraSim.Network;
namespace TerraSim.Simulation
{
    public class GroundTile : NamedObject
    {
        /// <summary>
        /// A backing field for the Wetness property. Do not use directly.
        /// </summary>
        private int _wetness = 0;
        /// <summary>
        /// Degree of wetness. Bounded for weather IntensityLevel.
        /// </summary>
        private int Wetness { get { return _wetness; } set { _wetness = value.Bound(0, 5); } }
        /// <summary>
        /// Maximal movement speed to be allowed on this tile.
        /// </summary>
        internal readonly int SpeedCap = 0;
        internal readonly int Darkening = 0;

        /// <summary>
        /// Degree of wetness.
        /// </summary>
        public IntensityLevel Humidity
        {
            get { return (IntensityLevel)_wetness; }
            set { Wetness = (int)value; }
        }
        public IntensityLevel Lighting { get; private set; }
        public int CurrentMaximalSpeed;

        /// <summary>
        /// Creates a new GroundTile.
        /// </summary>
        /// <param name="name">A unique name of the tile.</param>
        /// <param name="speedCap">Maximum speed allowed on the tile.</param>
        /// <param name="lighting">Initial lighting on the tile.</param>
        /// <param name="type">Type of the tile. (Used for clarity. Can be omitted.)</param>
        /// <param name="dispatcher">An object to be used as a dispatcher, 
        /// or null for default dispatcher.</param>
        public GroundTile(string name, int speedCap, IntensityLevel lighting, 
            string type = "", object dispatcher = null)
            : base(name)
        {
            this.SpeedCap = speedCap;
            Lighting = lighting;
            Type = type;
            this.DispatchMediator = dispatcher ?? NamedObject.defaultMediator;
        }
        
        public void IncreaseHumidity(int levels)
        {
            Wetness += levels;
        }

        public override string ToString()
        {
            return Type;
        }

        #region SimulationObject Members

        override public void Marshal(DataCollection data)
        {
            data.AddAttribute(Name, "humidity", Humidity.ToString());
            data.AddAttribute(Name, "lighting", Lighting.ToString());
            data.AddAttribute(Name, "position_x", PositionX.ToString());
            data.AddAttribute(Name, "position_y", PositionY.ToString());
            data.AddAttribute(Name, "type", Type);
        }

        public override void Update(WorldUpdateEventArgs args)
        {
            switch (args.World.CurrentWeather)
            {
                case WeatherType.Sunny:
                    Wetness--;
                    break;
                case WeatherType.Cloudy:
                    break;
                case WeatherType.Rainy:
                    Wetness++;
                    break;
                default:
                    break;
            }
            Lighting = (IntensityLevel)((((int)args.World.LightLevel) - Darkening))
                .Bound(1, 5);
        }

        #endregion

    }

}
