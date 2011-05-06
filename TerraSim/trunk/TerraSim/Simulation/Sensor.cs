
using TerraSim.Network;
namespace TerraSim.Simulation
{
    public abstract class Sensor : ISimulationObject
    {
        protected DataCollection LastInput = new DataCollection();
        protected ServiceMediator Mediator { get; private set; }

        public Sensor(UserAgent owner, ServiceMediator mediator = null)
        {
            Owner = owner;
            Mediator = mediator;
        }

        /// <summary>
        /// Gets the sensor's owning agent.
        /// </summary>
        public UserAgent Owner { get; private set; }

        /// <summary>
        /// Executes the sensor's work.
        /// </summary>
        /// <param name="args">Contains the data necessary for the sensor operation.</param>
        /// <param name="data">DataCollecion used to output the sensed data.</param>
        public abstract void Sense(WorldUpdateEventArgs args);

        #region SimulationObject Members

        /// <summary>
        /// Provides default behaviour for sensors. Adds the last data 
        /// sensed by this sensor.
        /// </summary>
        /// <param name="data">Collection to add data to.</param>
        public virtual void Marshal(DataCollection data)
        {
            data.AddFrom(LastInput);
        }

        #endregion

    }
}
