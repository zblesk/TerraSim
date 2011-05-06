using TerraSim.Simulation;

namespace TerraSim.VacuumBot
{
    class Camera : Sensor
    {
        public Camera(UserAgent owner) 
            : base(owner)
        {
        }

        public override void Sense(WorldUpdateEventArgs args)
        {
            LastInput.Clear();
            foreach (var hex in args.World.Map.EnumerateNeighbours(Owner.PositionX, Owner.PositionY, 2))
            {
                foreach (var obj in hex)
                {
                    obj.Marshal(LastInput);
                }
            }
            foreach (var obj in args.World.Map[Owner.PositionX, Owner.PositionY])
            {
                if (obj != this.Owner)
                {
                    obj.Marshal(LastInput);
                }
            }
        }
    }
}
