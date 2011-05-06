using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerraSim;

namespace Tests
{
    [TestClass]
    public class ExtensionMethods
    {
        [TestMethod]
        public void BoundNumberTest()
        {
            int x = 5;
            Assert.AreEqual(5, x.Bound(-4, 13));
            Assert.AreEqual(2, x.Bound(-4, 2));
            Assert.AreEqual(0, x.Bound(0, 0));
            x += 30;
            Assert.AreEqual(13, x.Bound(-4, 13));
            Assert.AreEqual(-1, -1.Bound(-4, 13));
            Assert.AreEqual(-5, (-40).Bound(-5, 20));
        }

        [TestMethod]
        public void MoveTest()
        {
            var x = new LinkedList<int>(new int[] { 0, 1, 2, 3 });
            var y = new LinkedList<int>(new int[] { 4, 5, 6, 7 });
            x.Move(y);
            Assert.AreEqual(0, y.Count);
            Assert.AreEqual(8, x.Count);
            var n = x.First;
            int value = 0;
            Assert.AreNotEqual(null, n);
            while (n != null)
            {
                Assert.AreEqual(value++, n.Value);
                n = n.Next;
            }
            x.Move(x);
            Assert.AreEqual(8, x.Count);
            x.Move(y);
            Assert.AreEqual(8, x.Count);
        }
    }
}
