using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TerraSim.Network
{
    internal class NetworkServer
    {
        public delegate void MessageEventHandler(int clientId, Message message);
        public delegate void ClientEventHandler(TcpClient sender, int clientId);

        private TcpListener tcpListener;
        private Thread listenThread;
        private Dictionary<int, Thread> spawnedThreads = new Dictionary<int, Thread>();
        private Dictionary<int, TcpClient> clients = new Dictionary<int, TcpClient>();
        private bool isRunninng = false;
        private const int bufferSize = 512;
        private int clientId = 0;
        private object threadLock = new object();

        /// <summary>
        /// Default port for the server.    
        /// </summary>
        public const int DefaultPort = 8183;
        public int Port { get; private set; }
        /// <summary>
        /// True if the server is currently running.
        /// </summary>
        public bool IsOnline { get { return isRunninng; } }
        /// <summary>
        /// Number of active clients.
        /// </summary>
        public int ClientCount { get; private set; }

        /// <summary>
        /// Client connected to the server.
        /// </summary>
        public event ClientEventHandler ClientConnected;
        /// <summary>
        /// Client disconnected from server.
        /// </summary>
        public event ClientEventHandler ClientDisconnected;
        /// <summary>
        /// Client received a message.
        /// </summary>
        public event MessageEventHandler MessageReceived;
        
        /// <summary>
        /// Creates a new instance of the server.
        /// </summary>
        /// <param name="port">Number of the port the server will listen at.</param>
        public NetworkServer(int port = DefaultPort)
        {
            Port = port;
        }

        /// <summary>
        /// Start the simulation.
        /// </summary>
        /// <param name="portNumber">Number of the port to listen at.</param>
        public void Start()
        {
            if (isRunninng)
            {
                return;
            }
            tcpListener = new TcpListener(IPAddress.Any, Port);
            listenThread = new Thread(new ThreadStart(ListenForClients));
            listenThread.Name = "Network server listening thread";
            listenThread.IsBackground = true;
            listenThread.Start();
            isRunninng = true;
            Trace.TraceInformation(string.Format("Network server started listening at port {0}",
                Port));
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        /// <remarks>Drops all active clients.</remarks>
        public void Stop()
        {
            if (!isRunninng)
            {
                Trace.TraceWarning("Network server is being stopped, though it was never started.");
                return;
            }
            isRunninng = false;
            listenThread.Abort();
            tcpListener.Stop();
            foreach (Thread t in spawnedThreads.Values)
            {
                t.Abort();
            }
            spawnedThreads.Clear();
            clients.Clear();
            Trace.TraceInformation("Network erver stopped.");
        }

        /// <summary>
        /// Sends a message to a client.
        /// </summary>
        /// <param name="clientId">ID of the message recipient.</param>
        /// <param name="message">Text of the message.</param>
        /// <param name="type">Type of the message.</param>
        /// <param name="format">Format of the message.</param>
        public void SendMessage(int clientId, string message, 
            MessageType type, MessageFormat format)
        {
            try
            {
                message = string.Format("{0}\n{1}\n{2}\n", type, format, message);
                int length = message.Length;
                message = string.Format("{0}\n{1}", length, message);
                Byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                NetworkStream stream = clients[clientId].GetStream();
                stream.Write(messageBytes, 0, messageBytes.Length);
                stream.Flush();
            }
            catch (InvalidOperationException)
            {
                // Occurs when the client was disconnected.
                Trace.TraceInformation("Client #{0} ({1}) left."
                    .Form(clientId, clients[clientId].Client.RemoteEndPoint.ToString()));
            }
        }

        /// <summary>
        /// Disconnects a client.
        /// </summary>
        /// <param name="clientId">ID of the client to be disconnected.</param>
        public void KickClient(int clientId)
        {
            TcpClient c = clients[clientId];
            SendMessage(clientId, "", MessageType.Exit, MessageFormat.Settings);
            OnClientDisonnected(c, clientId);
            Trace.TraceWarning(string.Format("Kicked client #{0} ({1})", clientId,
                c.Client.RemoteEndPoint.ToString()));
            c.Close();
        }

        /// <summary>
        /// Checks whether a client with the provided ID is connected.
        /// </summary>
        /// <param name="clientId">ID of the client to be checked.</param>
        /// <returns>Returns true if the client is connected, otherwise false.</returns>
        public bool IsClientConnected(int clientId)
        {
            return clients.ContainsKey(clientId);
        }

        private void ListenForClients()
        {
            this.tcpListener.Start();
            while (true)
            {
                TcpClient client = this.tcpListener.AcceptTcpClient();
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClient));
                lock (threadLock)
                {
                    clientId++;
                    spawnedThreads[clientId] = clientThread;
                    clientThread.Start(Tuple.Create<TcpClient, int>(client, clientId));
                    OnClientConnected(client, clientId);
                }
            }
        }

        private void HandleClient(object init)
        {
            Tuple<TcpClient, int> data = (Tuple<TcpClient, int>)init;
            NetworkStream clientStream = data.Item1.GetStream();
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while (true)
            {
                bytesRead = 0;
                try
                {
                    bytesRead = clientStream.Read(buffer, 0, bufferSize);
                }
                catch (IOException)
                {
                    Trace.TraceInformation("Client {0} shut down.", data.Item2.ToString());
                    OnClientDisonnected(data.Item1, data.Item2);
                    break;
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
                HandleReceivedMessage(data.Item2, Encoding.UTF8.GetString(buffer, 0, bytesRead));
            }
            data.Item1.Close();
            OnClientDisonnected(data.Item1, data.Item2);
        }

        #region Event triggers

        private void HandleReceivedMessage(int clientId, string message)
        {
            Message msg;
            string remainder = String.Empty;
            if ((MessageReceived != null))
            {
                while ((message.Length > 0) && Message.Parse(message, out msg, out remainder))
                {
                    MessageReceived(clientId, msg);
                    message = remainder;
                }
                if (remainder.Length > 0)
                {
                    throw new Exception("Unhandled case (yet).");
                }
            }
        }

        private void OnClientConnected(TcpClient socket, int clientId)
        {
            ClientCount++;
            clients[clientId] = socket;
            SendMessage(clientId, string.Format("your_id={0}", clientId.ToString()),
                MessageType.Settings, MessageFormat.Settings);
            if (ClientConnected != null)
            {
                ClientConnected(socket, clientId);
            }
            Trace.TraceInformation(string.Format("Client #{0} (from {1}) connected.",
                clientId, socket.Client.RemoteEndPoint.ToString()));
        }

        private void OnClientDisonnected(TcpClient socket, int clientId)
        {
            ClientCount--;
            clients.Remove(clientId);
            Thread t = spawnedThreads[clientId];
            spawnedThreads.Remove(clientId);
            if (ClientDisconnected != null)
            {
                ClientDisconnected(socket, clientId);
            }
            Trace.TraceInformation(string.Format("network: Client #{0} left.", clientId));
            t.Abort();
        }


        #endregion


    }
}
