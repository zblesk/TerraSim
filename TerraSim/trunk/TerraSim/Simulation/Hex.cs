using System.Collections.Generic;
using TerraSim.Network;

namespace TerraSim.Simulation
{
    public class Hex : ISimulationObject, IEnumerable<NamedObject>
    {
        public List<NamedObject> Objects { get; private set; }

        public static Hex Empty()
        {
            return new Hex { Objects = new List<NamedObject>() };
        }

        public Hex()
        {
            Objects = new List<NamedObject>();
        }

        public Hex(GroundTile tile)
            : this()
        {
            Objects.Add(tile);
        }

        public override string ToString()
        {
            if (Objects.Count == 0)
                return base.ToString();
            return "Position: {0}, {1}".Form(Objects[0].PositionX, Objects[0].PositionY);
        }

        #region SimulationObject Members

        public void Marshal(DataCollection data)
        {
            foreach (var @object in Objects)
            {
                @object.Marshal(data);
            }
        }

        #endregion

        #region IEnumerable<ISimulationObject> Members

        public IEnumerator<NamedObject> GetEnumerator()
        {
            return Objects.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Objects.GetEnumerator();
        }

        #endregion
    }
}
