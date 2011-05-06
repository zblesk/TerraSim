using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Entities
{
    public abstract class Plant : NamedObject
    {
        internal class PlantMediator
        {
            protected readonly Plant owner;

            internal PlantMediator(Plant owner)
            {
                this.owner = owner;
            }

            internal void Die()
            {
                owner.CanGrow = false;
            }

            internal void WasPickedUp()
            {
                Die();
            }
        }

        protected abstract bool HasChlorophyll { get; }
        protected abstract bool IsHydrophylic { get; }
        protected abstract int Size { get; set; }
        protected bool CanGrow { get; set; }
        public abstract bool IsPoisonous { get; }

        public Plant(string name) : base(name)
        {
            CanGrow = true;
        }

        public override void Marshal(DataCollection dataCollection)
        {
            base.Marshal(dataCollection);
            dataCollection.AddAttribute(Name, "size", Size.ToString());
        }
    }
}
