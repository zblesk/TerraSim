using System;
using System.Diagnostics;
using System.IO;

namespace TerraSim.Network
{

    public enum MessageType
    {
        Invalid = 0,
        Join = 1,
        Command = 2, 
        StateUpdate = 3,
        Exit = 4,
        Settings = 5,
        RequestStatistics = 6,
        Capabilities = 7
    }

    public enum MessageFormat
    {
        Invalid = 0,
        Predicate = 1,
        JSON = 2, 
        Settings = 9
    }
    
    public struct Message
    {
        public MessageType Type { get; internal set; }
        public string Body { get; internal set; }
        public MessageFormat Format { get; internal set; }
        /// <summary>
        /// Represents a invalid message.
        /// </summary>
        public static readonly Message InvalidMessage;

        /// <summary>
        /// Gets the total number of parsed messages.
        /// </summary>
        public static int MessagesParsed { get; private set; }

        static Message()
        {
            InvalidMessage = new Message(MessageType.Invalid,
                MessageFormat.Invalid, "");
        }

        public Message(MessageType type = MessageType.Invalid,
            MessageFormat format = MessageFormat.Invalid, 
            string body = "")
            : this()
        {
            Type = type;
            Format = format;
            Body = body;
        }

        /// <summary>
        /// Extracts the first message from the input string, returns the parsed message, 
        /// failure flag and the string remainder
        /// </summary>
        /// <param name="text">Test to parse.</param>
        /// <param name="message">Output message. Message.InvalidMessage if parsing failed.</param>
        /// <param name="remainder">Unprocessed part of the text.</param>
        /// <returns>True if the parsing was successful, otherwise false.</returns>
        public static bool Parse(string text, out Message message, out string remainder)
        {
            StringReader reader = new StringReader(text);
            string line = reader.ReadLine().Trim();
            int length;
            message = Message.InvalidMessage;
            if (!int.TryParse(line, out length) || (length <= 0))
            {
                // invalid message beginning
                remainder = reader.ReadToEnd();
                return false;
            }
            char[] data = new char[length];
            if (reader.ReadBlock(data, 0, length) != length)
            {
                // not a whole message was available; some part is missing.
                remainder = text;
                return false;
            }
            MessagesParsed++;
            remainder = reader.ReadToEnd();
            if (remainder.Length > 0)
            {
                int dummy = remainder.IndexOf('\n');
                if ((dummy > -1)
                    && !int.TryParse(remainder.Substring(0, dummy), out dummy))
                {
                    //damaged message data; discard the remainder.
                    remainder = string.Empty;
                    Trace.TraceWarning("Erroneous remainder encountered. (in Message.Parse(.))");
                }
            }
            StringReader msg = new StringReader(new string(data));
            MessageType msgType;
            if (!Enum.TryParse(msg.ReadLine().Trim(), true, out msgType))
            {
                //invalid message type
                return false;
            }
            MessageFormat msgFormat = MessageFormat.Invalid;
            if (!Enum.TryParse(msg.ReadLine().Trim(), true, out msgFormat))
            {
                //invalid format specified
                return false;
            }
            message = new Message(msgType, msgFormat);
            message.Body = msg.ReadToEnd();
            return true;
        }

        public bool IsEqual(Message msg)
        {
            return (this.Body == msg.Body) && (this.Format == msg.Format)
                && (this.Type == msg.Type);
        }
    }

}
