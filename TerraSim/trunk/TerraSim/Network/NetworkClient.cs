using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TerraSim.Network
{
    public class NetworkClient
    {
        private TcpClient client = null;
        private NetworkStream networkStream = null;
        private int bufferSize = 4096;
        private Thread listenThread = null;
        private string msgRemainder = string.Empty;
        
        /// <summary>
        /// Gets true if the client is connected to a server, otherwise false.
        /// </summary>
        public bool IsConnected { get { return client == null ? false : client.Connected; } }
        public event Action<string> MessageReceived;
        public event Action<Message> MessageParsed;
        public event EventHandler Connected;
        public event EventHandler Disconnected;

        /// <param name="address">Server address to connect to.</param>
        /// <param name="port">Port to connect to.</param>
        public void Connect(IPAddress address, int port)
        {
            if (client == null)
            {
                client = new TcpClient();
            }
            if (client.Connected)
            {
                return;
            }
            client.Connect(address, port);
            networkStream = client.GetStream();
            listenThread = new Thread(new ThreadStart(HandleServerMessages));
            listenThread.Name = "Client listening thread";
            listenThread.IsBackground = true;
            listenThread.Start();
            OnConnected();
        }

        /// <summary>
        /// If the client is connected, it disconnects.
        /// </summary>
        /// <remarks>If it is not connected, nothing happens. Multiple calls to 
        /// this functions have no effect.</remarks>
        public void Disconnect()
        {
            if (client.Connected)
            {
                SendMessage("", MessageType.Exit, MessageFormat.Settings);
                client.Client.Disconnect(false);
                OnDisconnected();
                listenThread.Abort();
                networkStream = null;
            }
        }

        public void JoinSimulation()
        {
            SendMessage("", MessageType.Join, MessageFormat.Settings);
        }

        /// <summary>
        /// Sends a message. 
        /// </summary>
        /// <param name="message">Text of the message.</param>
        /// <param name="type">Type of the message.</param>
        /// <param name="format">Format of the message.</param>
        /// <returns>Returns true on success, false on failure (e.g. when the client 
        /// is not connected.</returns>
        public bool SendMessage(string message, MessageType type, MessageFormat format)
        {
            if (!IsConnected)
            { 
                return false; 
            }
            message = string.Format("{0}\n{1}\n{2}", type, format, message);
            int length = message.Length;
            message = string.Format("{0}\n{1}", length, message);
            Byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            networkStream.Write(messageBytes, 0, messageBytes.Length);
            networkStream.Flush();
            return true;
        }

        private void HandleServerMessages()
        {
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while (true)
            {
                bytesRead = 0;
                try
                {
                    bytesRead = networkStream.Read(buffer, 0, bufferSize);
                }
                catch (SocketException ex)
                {
                    Trace.TraceError(ex.ToString());
                    break;
                }
                if (bytesRead == 0) // connection terminated
                {
                    break;
                }
                int count = 0;
                while ((count < bytesRead) && (buffer[count] != 0))
                {
                    count++;
                }
                OnMessageReceived(Encoding.UTF8.GetString(buffer, 0, count));
            }
        }

        #region Event triggers

        private void OnMessageReceived(string message)
        {
            if (MessageReceived != null)
            {
                MessageReceived(message);
            }
            Message m;
            if (Message.Parse(msgRemainder + message, out m, out msgRemainder))
            {
                OnMessageParsed(m);
            }
        }

        private void OnConnected()
        {
            if (Connected != null)
            {
                Connected(this, EventArgs.Empty);
            }
        }

        private void OnDisconnected()
        {
            if (Disconnected != null)
            {
                Disconnected(this, EventArgs.Empty);
            }
        }

        private void OnMessageParsed(Message m)
        {
            if (MessageParsed != null)
            {
                MessageParsed(m);
            }
        }

        #endregion

    }
}
