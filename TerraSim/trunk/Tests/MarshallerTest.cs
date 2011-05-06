using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerraSim;
using TerraSim.Network;
namespace Tests
{
    
    [TestClass()]
    public class MarshallerTest
    {

        [TestMethod()]
        public void PredicateMarshallerTest()
        {
            DataCollection target = new DataCollection();
            Assert.AreEqual(string.Empty, Marshallers.ToPredicates(target));
            target.AddAction("a1", "action", "par1", "par2");
            Assert.AreEqual("performs_action (a1, action, par1, par2)\r\n", 
                Marshallers.ToPredicates(target));
            target.AddAttribute("target", "attName", "attValue");
            Assert.AreEqual("performs_action (a1, action, par1, par2)\r\n"
                + "has_attribute (target, attName, attValue)\r\n", 
                Marshallers.ToPredicates(target));
            target.AddAction("a2", "action", "par1", "par4");
            Assert.AreEqual("performs_action (a1, action, par1, par2)\r\n"
                + "performs_action (a2, action, par1, par4)\r\n"
                + "has_attribute (target, attName, attValue)\r\n", 
                Marshallers.ToPredicates(target));
            target.Clear();
            Assert.AreEqual(string.Empty, Marshallers.ToPredicates(target));
        }

        [TestMethod()]
        public void JSONMarshallerTest()
        {
            DataCollection target = new DataCollection();
            const string empty = "{\"has_attribute\":[],\"performs_action\":[]}";
            Assert.AreEqual(empty, Marshallers.ToJSON(target, false));
            target.AddAction("a1", "action", "par1", "par2");
            Assert.AreEqual("{\"has_attribute\":[],\"performs_action\":[[\"a1\",\"action\",\"par1\",\"par2\"]]}",
                Marshallers.ToJSON(target, false));
            target.AddAttribute("target", "attName", "attValue");
            Assert.AreEqual(
                "{\"has_attribute\":[[\"target\",\"attName\",\"attValue\"]],\"performs_action\":[[\"a1\",\"action\",\"par1\",\"par2\"]]}",
                Marshallers.ToJSON(target, false));            
            target.AddAction("a2", "action", "par1", "par4");
            Assert.AreEqual(
                "{\"has_attribute\":[[\"target\",\"attName\",\"attValue\"]],\"performs_action\":[[\"a1\",\"action\",\"par1\",\"par2\"],[\"a2\",\"action\",\"par1\",\"par4\"]]}",
                Marshallers.ToJSON(target, false));
            target.AddAttribute("t2", "a2", "v2");
            Assert.AreEqual(
                "{\"has_attribute\":[[\"target\",\"attName\",\"attValue\"],[\"t2\",\"a2\",\"v2\"]],\"performs_action\":[[\"a1\",\"action\",\"par1\",\"par2\"],[\"a2\",\"action\",\"par1\",\"par4\"]]}",
                Marshallers.ToJSON(target, false));
            target.Clear();
            Assert.AreEqual(empty, Marshallers.ToJSON(target, false));
            target.AddAttribute("x", "y", "z");
            Assert.AreEqual("{\"has_attribute\":[[\"x\",\"y\",\"z\"]],\"performs_action\":[]}",
                Marshallers.ToJSON(target, false));
        }

    }
}
