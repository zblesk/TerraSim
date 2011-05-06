using System;
using System.Collections.Generic;
using System.Linq;

namespace TerraSim.Simulation
{
    public sealed class UserAgent : Agent
    {
        /// <summary>
        /// Agent's ID in the simulation.
        /// </summary>
        public int Id { get; private set; }
        private Dictionary<string, Actuator> actionBindings;
                
        public UserAgent(string name, int id, object mediator = null)
            : base(name)
        {
            DispatchMediator = mediator ?? NamedObject.defaultMediator;
            Id = id;
            actionBindings = new Dictionary<string, Actuator>(StringComparer.OrdinalIgnoreCase);
            Type = "user_agent";
        }

        /// <summary>
        /// Gets a list of action names of all the actions this Agent
        /// is able to perform.
        /// </summary>
        /// <returns>Returns a list of available actions.</returns>
        public IEnumerable<string> GetPossibleActions()
        {
            return from action in actionBindings.Keys select action;
        }

        /// <summary>
        /// Performs an action.
        /// </summary>
        /// <param name="action">Name of the action to perform.</param>
        /// <returns>Returns true if the action can be performed (there are right actuators),
        /// otherwise false.</returns>
        public bool PerformAction(string action, string argument1,
            string argument2, WorldUpdateEventArgs args)
        {
            Actuator act;
            if (!actionBindings.TryGetValue(action, out act))
            {
                return false; //unknown action
            }
            act.Perform(args, action, argument1, argument2);
            return true;
        }

        protected override void OnActuatorAdded(Actuator actuator)
        {
            foreach (string name in actuator.ActionList())
            {
                actionBindings[name] = actuator;
            }
        }
    }
}
