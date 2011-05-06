using TerraSim.Network;

namespace TerraSim.Simulation
{
    public struct ServerSettings
    {
        static readonly private ServerSettings defaults;

        public int NetworkPort;
        /// <summary>
        /// Length of one day in minutes.
        /// </summary>
        public int DayCycleLength;

        static ServerSettings()
        {
            defaults.NetworkPort = NetworkServer.DefaultPort;
            defaults.DayCycleLength = 5;
        }

        public static ServerSettings Default { get { return defaults; } }
    }
}
