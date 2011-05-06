using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TerraSim;
using TerraSim.Network;

namespace Tests
{
    
    [TestClass()]
    public class MessageTest
    {

        /// <summary>
        ///A test for Parse
        ///</summary>
        [TestMethod()]
        public void ParseTest()
        {
            Assert.AreEqual(Message.MessagesParsed, 0);
            string text = "34\n2\n1\nhasattribute(dummy, att, val)\n";
            Message message = Message.InvalidMessage;
            Message messageExpected = new Message(MessageType.Command,
                MessageFormat.Predicate, "hasattribute(dummy, att, val)\n");
            string remainder = string.Empty; 
            string remainderExpected = string.Empty; 
            bool expected = true;
            bool actual;
            actual = Message.Parse(text, out message, out remainder);
            Assert.IsTrue(message.IsEqual(messageExpected));
            Assert.AreEqual(remainderExpected, remainder);
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(Message.MessagesParsed, 1);

            text = "34\n2\n1\nhasattribute(dum";
            actual = Message.Parse(text, out message, out remainder);
            Assert.AreEqual(Message.InvalidMessage, message);
            Assert.AreEqual(text, remainder);
            Assert.AreEqual(false, actual);
            Assert.AreEqual(Message.MessagesParsed, 1);


            text = "34\n4\n2\nhasattribute(dummy, att, val)\n34\n2\n1\nhasattribute(dummy, att, val)\n";
            Message expected2 = new Message(MessageType.Exit, MessageFormat.JSON,
                "hasattribute(dummy, att, val)\n");
            actual = Message.Parse(text, out message, out remainder);
            Assert.IsTrue(expected2.IsEqual(message));
            Assert.AreEqual("34\n2\n1\nhasattribute(dummy, att, val)\n", remainder);
            Assert.AreEqual(true, actual);

            text = "49\nstateupdate\nJson\nhasattribute(dummy, att, val)\n\n\n";
            Message expected3 = new Message(MessageType.StateUpdate, MessageFormat.JSON,
                "hasattribute(dummy, att, val)\n\n\n");
            actual = Message.Parse(text, out message, out remainder);
            Assert.IsTrue(expected3.IsEqual(message));
            Assert.AreEqual(remainder, String.Empty);
            Assert.AreEqual(true, actual);
            Assert.AreEqual(Message.MessagesParsed, 3);
        }
    }
}
