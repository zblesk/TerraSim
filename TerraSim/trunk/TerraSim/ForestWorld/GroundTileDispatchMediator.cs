using TerraSim.Simulation;

namespace TerraSim.ForestWorld
{
    internal class GroundTileDispatchMediator
    {
        private readonly GroundTile owner;

        public GroundTileDispatchMediator(GroundTile owner)
        {
            this.owner = owner;
        }

        private void WasWatered()
        {
            owner.IncreaseHumidity(2);
        }

        private void WasSteppedOn(Agent sender)
        {
        }
    }
}
