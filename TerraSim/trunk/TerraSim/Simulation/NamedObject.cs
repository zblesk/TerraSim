using System;
using TerraSim.Network;

namespace TerraSim.Simulation
{
    public abstract class NamedObject : ISimulationObject
    {
        protected static readonly object defaultMediator = new object();
        /// <summary>
        /// A background field for DispatchMediator.
        /// </summary>
        private object _dispatchMediator = NamedObject.defaultMediator;

        /// <summary>
        /// Name of the object.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Additional info about the object.
        /// </summary>
        public virtual string Type { get; set; }
        public int PositionX { get; internal protected set; }
        public int PositionY { get; internal protected set; }
        /// <summary>
        /// An object that handles dynamically dispatched calls used in 
        /// simulation communication, if any is provided.
        /// </summary>
        /// <remarks>If null is provided to the setter, the default dispatch
        /// mediator is used instead.</remarks>
        public object DispatchMediator
        {
            get { return _dispatchMediator; }
            set { _dispatchMediator = value ?? NamedObject.defaultMediator; }
        }

        /// <summary>
        /// Gathers all the data relevant for this instance, to be sent 
        /// across the network.
        /// </summary>
        /// <param name="dataCollection">DataCollection to add data into.</param>
        /// <remarks>Sends the this.Type of this instance.</remarks>
        public virtual void Marshal(DataCollection dataCollection)
        {
            dataCollection.AddAttribute(Name, "position_x", PositionX.ToString());
            dataCollection.AddAttribute(Name, "position_y", PositionY.ToString());
            dataCollection.AddAttribute(Name, "type", Type.ToString());
        }

        /// <summary>
        /// Creates a new NamedObject.
        /// </summary>
        /// <param name="name">Unique name of the object in simulation.</param>
        /// <param name="mediator">An object handling </param>
        public NamedObject(string name, object mediator = null)
        {
            this.Name = name;
            DispatchMediator = mediator ?? defaultMediator;
            Type = "No type specified for {0} ({1})".Form(this.Name, this.Type);
        }

        public abstract void Update(WorldUpdateEventArgs args);
    }
}
