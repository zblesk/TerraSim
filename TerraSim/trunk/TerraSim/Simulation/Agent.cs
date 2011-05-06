using System;
using System.Collections.Generic;
using TerraSim.Network;

namespace TerraSim.Simulation
{
    public class Agent : NamedObject
    {
        private List<Actuator> actuators;
        private List<Sensor> sensors;

        public Agent(string name)
            : base(name)
        {
            sensors = new List<Sensor>();
            actuators = new List<Actuator>();
            Type = "user_agent";
        }

        public void AddActuator(Actuator actuator)
        {
            if (actuator.Owner != this)
            {
                throw new Exception("Can not add an actuator owned by another agent.");
            }
            actuators.Add(actuator);
            OnActuatorAdded(actuator);
        }

        public void AddSensor(Sensor sensor)
        {
            if (sensor.Owner != this)
            {
                throw new Exception("Can not add a sensor owned by another agent.");
            }
            sensors.Add(sensor);
        }
        
        public void UpdateSensors(WorldUpdateEventArgs args)
        {
            foreach (var sensor in sensors)
            {
                sensor.Sense(args);
            }
        }

        public void RemoveSensors()
        {
            sensors.Clear();
        }

        public void RemoveActuators()
        {
            actuators.Clear();
        }

        /// <summary>
        /// Can be used by deriving classes to define additional behaviour.
        /// </summary>
        /// <param name="actuator">Actuator being added.</param>
        protected virtual void OnActuatorAdded(Actuator actuator) { }

        #region SimulationObject Members

        public override void Marshal(DataCollection dataCollection)
        {
            base.Marshal(dataCollection);
            foreach (Actuator act in actuators)
            {
                act.Marshal(dataCollection);
            }
            foreach (Sensor sen in sensors)
            {
                sen.Marshal(dataCollection);
            }
        }

        public override void Update(WorldUpdateEventArgs args)
        {
            foreach (var act in actuators)
            {
                act.Update(args);
            }
        }

        #endregion
    }
}
