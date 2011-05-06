using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerraSim.Simulation;

namespace Tests
{
    [TestClass()]
    public class MessageBroadcasterTest
    {
        [TestMethod()]
        public void SubscribeTest()
        {
            MessageBroadcaster target = new MessageBroadcaster();
            string toMessage = "test_msg";
            int state = 0;
            Action<object> action = (o) => { state++; };
            Agent recipient = new Agent("test_agent");
            target.Broadcast(toMessage, null, recipient);
            Assert.AreEqual(0, state);
            target.Subscribe(toMessage, action, recipient);
            Assert.AreEqual(0, state);
            target.Broadcast(toMessage, null, recipient);
            Assert.AreEqual(1, state);
            target.Broadcast(toMessage, null, recipient);
            target.Broadcast(toMessage, null, recipient);
            Assert.AreEqual(3, state);
            target.Unsubscribe(toMessage, recipient);
            target.Broadcast(toMessage, null, recipient);
            target.Broadcast(toMessage, null, recipient);
            Assert.AreEqual(3, state);
        }

        [TestMethod()]
        public void CallbackTest()
        {
            MessageBroadcaster target = new MessageBroadcaster();
            string toMessage = "callback_msg";
            int state = 0;
            Agent recipient = new Agent("test_agent_2");
            Action<object> callback = (o) => { state = (int)o; };
            target.Broadcast(toMessage, 3, recipient, callback);
            Assert.AreEqual(0, state);
            target.SubscribeWithCallback(toMessage,
                (o, c) =>
                {
                    int x = (int)o;
                    x = x * 2 + 2;
                    c(x);
                },
                recipient);
            target.Broadcast(toMessage, 3, recipient, callback);
            Assert.AreEqual(8, state);
            target.Broadcast(toMessage, state, recipient, callback);
            Assert.AreEqual(18, state);

            Agent recipient2 = new Agent("test_agent_3");
            Agent recipient3 = new Agent("test_agent_4");
            int state2 = 0;
            int state3 = 0;
            Action<object> callback2 = (o) => { state2 = (int)o; };
            Action<object> callback3 = (o) => { state3 = (int)o; };
            target.SubscribeWithCallback(toMessage,
                (o, c) =>
                {
                    int x = (int)o;
                    x = (x - 2) * 2;
                    c(x);
                },
                recipient2);
            target.Subscribe(toMessage, callback3, recipient3);
            target.Broadcast(toMessage, 3, recipient2, callback2);
            Assert.AreEqual(18, state);
            Assert.AreEqual(2, state2);
            Assert.AreEqual(3, state3);
            target.Broadcast(toMessage, 5, recipient, callback);
            Assert.AreEqual(6, state);
            Assert.AreEqual(2, state2);
            Assert.AreEqual(5, state3);
        }

    }
}
