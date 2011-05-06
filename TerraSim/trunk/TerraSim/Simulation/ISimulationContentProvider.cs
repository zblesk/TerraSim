using System;
using System.IO;

namespace TerraSim.Simulation
{
    public interface ISimulationContentProvider
    {

        /// <summary>
        /// The maximum number of clients the simulation can support.
        /// </summary>
        int MaxClients { get; }
        /// <summary>
        /// Specifies whether this simulation logic provider is capable of generating
        /// random maps.
        /// </summary>
        bool CanGenerateRandomMaps { get; }
        /// <summary>
        /// Loads a map for the world.
        /// </summary>
        /// <param name="inputStream">Stream with the JSON definition of the map.</param>
        /// <param name="settings">Settings of the world.</param>
        /// <returns>Returns a new instance of the World using the given map.</returns>
        World LoadMap(Stream inputStream, WorldSettings settings);
        /// <summary>
        /// Generates a random map, if such functionality is supported.
        /// </summary>
        /// <param name="settings">Settings of the world</param>
        /// <param name="weatherModel">Markov model of the weather (or null if not used.)</param>
        /// <returns>Returns a new instance of the World with a random map.</returns>
        World RandomMap(WorldSettings settings, MarkovModel weatherModel);
        
        /// <summary>
        /// Generates an agent with sensors and actuators.
        /// </summary>
        /// <returns>Returns a tuple containing the created agent 
        /// and its X and Y positions in the world.</returns>
        Tuple<UserAgent, int, int> GenerateAgent(int agentId);

    }
}
