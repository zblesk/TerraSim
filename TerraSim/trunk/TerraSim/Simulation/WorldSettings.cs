
namespace TerraSim.Simulation
{
    /// <summary>
    /// Used when constructing a World instance. Contains the initialization settings, 
    /// which can not be changed later.
    /// </summary>
    public struct WorldSettings
    {
        private readonly static WorldSettings defaults;
        public bool WeatherEnabled;
        public bool DayNightEnabled;
        public int BarometricPressure;
        /// <summary>
        /// Time at which the sun begins to rise.
        /// </summary>
        public ushort Dawn;
        /// <summary>
        /// Time at which the sun begins to set. 
        /// </summary>
        public ushort Dusk;
        /// <summary>
        /// Represents, how many time units it takes to turn the light from day to 
        /// night, et vice versa.
        /// </summary>
        public ushort LightFadeSpan;
        /// <summary>
        /// How many times the MM changes its state per one day. 
        /// </summary>
        public uint WeatherStateTransitionsPerDay;
        /// <summary>
        /// Indicates how many parts will a day be divided into.
        /// </summary>
        public int DayPartCount;

        /// <summary>
        /// Static constructor. Initializes default values of the structure.
        /// </summary>
        static WorldSettings()
        {
            defaults.Dawn = 25;
            defaults.Dusk = 75;
            defaults.LightFadeSpan = 10;
            defaults.WeatherStateTransitionsPerDay = 20;
            defaults.WeatherEnabled = true;
            defaults.DayNightEnabled = true;
            defaults.BarometricPressure = 70;
            defaults.DayPartCount = 100;
        }

        public static WorldSettings Default { get { return defaults; } }
    }

}
