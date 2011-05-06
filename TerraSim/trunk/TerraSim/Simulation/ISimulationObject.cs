using TerraSim.Network;

namespace TerraSim.Simulation
{
    public interface ISimulationObject
    {
        /// <summary>
        /// Get the object's representation.
        /// </summary>
        void Marshal(DataCollection dataCollection);
    }
}
