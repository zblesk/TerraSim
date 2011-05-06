using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerraSim;
using TerraSim.Network;

namespace Tests
{
    [TestClass]
    public class NetworkTest
    {
        readonly IPAddress localhost = IPAddress.Parse("127.0.0.1");
        public NetworkTest() { }
        
        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void NetworkOnOffTest()
        {
            int port = 4132;
            NetworkServer serv = new NetworkServer(port);
            Assert.IsFalse(serv.IsOnline);
            serv.Start();
            Assert.IsTrue(serv.IsOnline);
            Assert.AreEqual(0, serv.ClientCount);
            NetworkClient c1 = new NetworkClient();
            Assert.IsFalse(c1.IsConnected);
            c1.Connect(localhost, port);
            Assert.IsTrue(c1.IsConnected);
            c1.JoinSimulation();
            Thread.Sleep(1000); // not exactly ideal...
            Assert.AreEqual(1, serv.ClientCount);
            c1.Disconnect();
            Assert.IsFalse(c1.IsConnected);
            Thread.Sleep(1000);
            Assert.AreEqual(0, serv.ClientCount);
            Assert.IsTrue(serv.IsOnline);
            serv.Stop();
            Assert.IsFalse(serv.IsOnline);
        }

    }
}
