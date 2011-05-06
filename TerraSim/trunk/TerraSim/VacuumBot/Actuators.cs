using System.Linq;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.VacuumBot
{
    class Motor : Actuator
    {
        private enum Orientations { Up = 0, Right = 1, Down = 2, Left = 3 };
        private static int[][] movementOffsets = new int[][] { 
            new int[] {0, -1}, new int[] {1, 0}, new int[] {0, 1}, new int[] {-1, 0}};
        private Orientations orientation = Orientations.Up;


        public Motor(UserAgent owner) 
            : base(owner)
        {

        }

        public override string[] ActionList()
        {
            return new string[] { "left", "right", "forward" };
        }

        public override void Perform(WorldUpdateEventArgs args, string action,
            string argument1, string argument2)
        {
            switch (action.ToLower())
            {
                case ("right"):
                    Turn(1);
                    break;
                case ("left"):
                    Turn(-1);
                    break;
                case ("forward"):
                    var newX = Owner.PositionX + movementOffsets[(int)orientation][0];
                    var newY = Owner.PositionY + movementOffsets[(int)orientation][1];
                    if (!args.World.Map[newX, newY].Any(
                        o => (o.Type == "wall") || (o.Type == "agent")))
                    {
                        args.World.MoveObject(Owner, newX, newY);
                    }
                    break;
            }
        }

        public override void Marshal(DataCollection data)
        {
            data.AddAttribute(Owner.Name, "heading", orientation.ToString());
        }
        
        private void Turn(int direction)
        {
            int o = (int)orientation;
            o = (o + 4 + direction) % 4;
            orientation = (Orientations)o;
        }

        public override void Update(WorldUpdateEventArgs args)
        {
        }
    }

    public class Vacuum : Actuator
    {
        private int sucked = 0;
        private int thrown = 0;

        public Vacuum(UserAgent owner) 
            : base(owner)
        {
        }

        public override string[] ActionList()
        {
            return new string[] { "suck" };
        }


        public override void Perform(WorldUpdateEventArgs args, string action,
            string argument1, string argument2)
        {
            var objects = args.World.Map[Owner.PositionX, Owner.PositionY].Objects;
            var obj = from o in objects
                      where (o.Type == "dirt")
                      select o;
            var e = obj.GetEnumerator();
            if (!e.MoveNext())
            {
                var dirt = new InanimateObject("");
                dirt.Type = "dirt";
                objects.Add(dirt);
                thrown++;
            }
            else
            {
                objects.Remove(e.Current);
                sucked++;
            }
        }

        public override void Marshal(DataCollection data)
        {
            data.AddAttribute(Owner.Name, "dirt_sucked", sucked.ToString());
            data.AddAttribute(Owner.Name, "dirt_thrown", thrown.ToString());
        }

        public override void Update(WorldUpdateEventArgs args)
        {
        }
    }
}
