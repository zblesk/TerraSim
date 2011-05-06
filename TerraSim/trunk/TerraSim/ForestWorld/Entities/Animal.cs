using TerraSim.ForestWorld.Actuators;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Entities
{
    public class Animal : Agent
    {
        internal class Mediator
        {
            private readonly Animal owner;

            internal Mediator(Animal owner)
            {
                this.owner = owner;
            }

            internal void WasShot()
            {
                owner.Die();
            }
        }
        
        private bool dead = false;
        internal const int SightRange = 3;

        public Animal(string name, ServiceMediator mediator)
            : base(name)
        {
            var m = new Mediator(this);
            DispatchMediator = m;
            AddActuator(new DecisionCenter(this, mediator));
            Type = "animal";
        }

        public override void Update(WorldUpdateEventArgs args)
        {
            if (!dead)
            {
                base.Update(args);
            }
        }

        public override void Marshal(DataCollection dataCollection)
        {
            base.Marshal(dataCollection);
            if (dead)
            {
                dataCollection.AddAttribute(this.Name, "dead", "true");
            }
        }

        internal void Die()
        {
            dead = true;
            RemoveSensors();
            RemoveActuators();
        }
    }
}
