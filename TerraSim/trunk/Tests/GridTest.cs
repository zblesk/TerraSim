using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerraSim.Simulation;

namespace Tests
{
    /// <summary>
    /// Tests the components and the component manager.
    /// </summary>
    [TestClass]
    public class GridTest
    {
        [TestMethod]
        public void HexOperations()
        {
            Hex h = new Hex();
            Assert.IsNotNull(h.Objects);
            Hex w = Hex.Empty();
            Assert.AreNotEqual(w, h);
            Hex x = Hex.Empty();
            Assert.IsNotNull(x.Objects);
            Assert.AreEqual(0, x.Objects.Count);
        }

        [TestMethod]
        public void GridConstructionTest()
        {
            Grid grid = new Grid(3, 3);
            Hex h;
            Assert.IsTrue(grid.TryGet(0, 0, out h));
            Assert.IsNull(h);
            Assert.IsFalse(grid.TryGet(3, 3, out h));
            Assert.IsFalse(grid.TryGet(3, 4, out h));
            Assert.IsFalse(grid.TryGet(-1, 2, out h));
        }

        [TestMethod]
        public void GridHexNeighbourTest()
        {
            Grid_Accessor grid = new Grid_Accessor(
                new PrivateObject(new Grid(5, 4, Grid.GridMode.Hexagonal, true)));
            Assert.IsNotNull(grid.EnumerateHexNeighbours(2, 2));
            Assert.IsNull(grid.EnumerateHexNeighbours(5, 4));
            Assert.IsNotNull(grid.EnumerateHexNeighbours(0, 0));
            Assert.IsNull(grid.EnumerateHexNeighbours(-1, 0));
            Assert.IsNull(grid.EnumerateHexNeighbours(2, 10));
            Assert.AreNotEqual(null, grid[2, 2]);
            Assert.AreEqual(2, grid.EnumerateHexNeighbours(0, 0).Count);
            Assert.AreEqual(4, grid.EnumerateHexNeighbours(0, 1).Count);
            Assert.AreEqual(5, grid.EnumerateHexNeighbours(1, 0).Count);
            Assert.AreEqual(6, grid.EnumerateHexNeighbours(2, 1).Count);
            Assert.AreEqual(3, grid.EnumerateHexNeighbours(4, 3).Count);
            grid = new Grid_Accessor(
                new PrivateObject(new Grid(5, 4, Grid.GridMode.Hexagonal, false))); 
            Assert.AreEqual(1, grid.EnumerateHexNeighbours(0, 0).Count);
            Assert.AreEqual(1, grid.EnumerateHexNeighbours(0, 1).Count);
            Assert.AreEqual(1, grid.EnumerateHexNeighbours(1, 0).Count);
            Assert.AreEqual(1, grid.EnumerateHexNeighbours(2, 1).Count);
            Assert.AreEqual(1, grid.EnumerateHexNeighbours(4, 3).Count);
            Assert.AreEqual(null, grid[2, 2]);
        }


        [TestMethod]
        public void GridOctNeighbourTest()
        {
            var grid = new Grid(5, 4, Grid.GridMode.Octagonal, true); 
            Assert.IsNotNull(grid.EnumerateNeighbours(2, 2, 1));
            Assert.AreEqual(0, grid.EnumerateNeighbours(5, 4, 1).Count());
            Assert.IsNotNull(grid.EnumerateNeighbours(0, 0, 1));
            Assert.AreEqual(0, grid.EnumerateNeighbours(-1, 0, 1).Count());
            Assert.AreEqual(0, grid.EnumerateNeighbours(2, 10, 1).Count());
            Assert.AreNotEqual(null, grid[2, 2]);
            Assert.AreEqual(3, grid.EnumerateNeighbours(0, 0, 1).Count);
            Assert.AreEqual(5, grid.EnumerateNeighbours(0, 1, 1).Count);
            Assert.AreEqual(5, grid.EnumerateNeighbours(1, 0, 1).Count);
            Assert.AreEqual(8, grid.EnumerateNeighbours(2, 1, 1).Count);
            Assert.AreEqual(8, grid.EnumerateNeighbours(3, 2, 1).Count);
            Assert.AreEqual(3, grid.EnumerateNeighbours(4, 3, 1).Count);
            grid = new Grid(5, 4, Grid.GridMode.Octagonal, false);
            Assert.AreEqual(1, grid.EnumerateNeighbours(0, 0, 1).Count);
            Assert.AreEqual(1, grid.EnumerateNeighbours(0, 1, 1).Count);
            Assert.AreEqual(1, grid.EnumerateNeighbours(1, 0, 1).Count);
            Assert.AreEqual(1, grid.EnumerateNeighbours(2, 1, 1).Count);
            Assert.AreEqual(1, grid.EnumerateNeighbours(4, 3, 1).Count);
            Assert.AreEqual(null, grid[2, 2]);

            grid = new Grid(7, 7, Grid.GridMode.Octagonal, true);
            Assert.AreEqual(8, grid.EnumerateNeighbours(2, 2, 1).Count);
            Assert.AreEqual(24, grid.EnumerateNeighbours(2, 2, 2).Count);
            Assert.AreEqual(19, grid.EnumerateNeighbours(1, 2, 2).Count);
            Assert.AreEqual(8, grid.EnumerateNeighbours(0, 0, 2).Count);
            Assert.AreEqual(15, grid.EnumerateNeighbours(0, 0, 3).Count);
            Assert.AreEqual(48, grid.EnumerateNeighbours(2, 2, 4).Count);
        }

        [TestMethod]
        public void OctGridDistance()
        {
            var g = new Grid(1, 1, Grid.GridMode.Octagonal, false);
            Assert.AreEqual(0, g.Distance(1, 1, 1, 1));
            Assert.AreEqual(1, g.Distance(5, 6, 5, 5));
            Assert.AreEqual(1, g.Distance(4, 4, 5, 5));
            Assert.AreEqual(2, g.Distance(0, 0, 2, 2));
            Assert.AreEqual(2, g.Distance(0, 2, 2, 2));
            Assert.AreEqual(2, g.Distance(0, 1, 2, 2));
            Assert.AreEqual(4, g.Distance(3, 5, 1, 1));
        }

        [TestMethod]
        public void HexGridDistance()
        {
            var g = new Grid(1, 1, Grid.GridMode.Hexagonal, false);
            Assert.AreEqual(0, g.Distance(3, 3, 3, 3));
            Assert.AreEqual(4, g.Distance(0, 4, 0, 0));
            for (int i = 1; i <= 4; i++)
            {
                Assert.AreEqual(3, g.Distance(3, 2, 0, i));
            }
        }

    }
}
