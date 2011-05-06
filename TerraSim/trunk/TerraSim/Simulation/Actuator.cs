
using TerraSim.Network;
namespace TerraSim.Simulation
{
    public abstract class Actuator : ISimulationObject
    {
        /// <summary>
        /// Gets the actuator's owning agent.
        /// </summary>
        public Agent Owner { get; private set; }
        protected ServiceMediator Mediator { get; private set; }

        public Actuator(Agent owner, ServiceMediator mediator = null)
        {
            Owner = owner;
            Mediator = mediator;
        }

        /// <summary>
        /// Gets a list of all actions registered (carried out) by this actuator.
        /// </summary>
        /// <returns>Returns a list of all actions registered (carried out) by this actuator.</returns>
        public abstract string[] ActionList();

        /// <summary>
        /// Perform an action.
        /// </summary>
        /// <param name="args">Current state of the world.</param>
        /// <param name="action">Action name.</param>
        /// <param name="param">Action parameters.</param>
        public abstract void Perform(WorldUpdateEventArgs args, string action, 
            string argument1, string argument2);

        /// <summary>
        /// Updates the Actuator's state.
        /// </summary>
        /// <param name="args">Current state of the world.</param>
        /// <remarks>Call it in each time step to keep performing </remarks>
        public abstract void Update(WorldUpdateEventArgs args);

        #region SimulationObject Members

        public abstract void Marshal(DataCollection dataCollection);

        #endregion
    }
}
