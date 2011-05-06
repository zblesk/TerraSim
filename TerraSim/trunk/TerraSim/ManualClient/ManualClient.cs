using System;
using System.Collections.Generic;
using System.Net;

namespace TerraSim.ManualClient
{
    using System.Diagnostics;
    using ClientAction = Action<ManualClient, TerraSim.Network.Message>;
    using TerraSim.Network;

    /// <summary>
    /// This class provides a network client used to connect to a simulation 
    /// server and issue commands manually. It is intended for debugging and/or 
    /// exploratory purposes.
    /// </summary>
    public sealed class ManualClient
    {
        private NetworkClient client = new NetworkClient();
        private List<string> actionsList = new List<string>();

        public event ClientAction ActionListReceived,  
            PerceptReceived, SettingsReceived;
        public event EventHandler Connected, Disconnected;

        public bool IsConnected { get { return client.IsConnected; } }

        public ManualClient()
        {
            client.Connected += (s, e) => { client.JoinSimulation(); };
            client.Connected += (s, e) => { OnConnected(); };
            client.Disconnected += (s, e) => { OnDisconnected(); };
            client.MessageParsed += OnNewMessage;
        }

        /// <summary>
        /// Connects to a remote server.
        /// </summary>
        /// <param name="address">Address of the server.</param>
        /// <param name="port">Port to be used.</param>
        public void Connect(IPAddress address, int port = NetworkServer.DefaultPort)
        {
            client.Connect(address, port);
        }

        /// <summary>
        /// Disconnects from the server.
        /// </summary>
        public void Disconnect()
        {
            client.Disconnect();
        }

        private void OnNewMessage(Message message)
        {
            switch (message.Format)
            {
                case (MessageFormat.Settings):
                    OnSettingsMessage(message);
                    break;
                case (MessageFormat.JSON):
                case (MessageFormat.Predicate):
                    switch (message.Type)
                    {
                        case (MessageType.Capabilities):
                            OnActionListUpdated(message);
                            break;
                        case (MessageType.Command):
                        case (MessageType.Join):
                        case (MessageType.RequestStatistics):
                            Trace.TraceInformation(
                                "The client should not receive '{0}' messages. They will be ignored."
                                .Form(message.Type.ToString()));
                            break;
                        case (MessageType.StateUpdate):
                            OnPercept(message);
                            break;
                    }
                    break;
                default:
                    throw new Exception("Invalid mesage format.");
            }
        }

        public void SendMessage(string message, MessageType type,
            MessageFormat format)
        {
            client.SendMessage(message, type, format);
        }

        #region Event triggers
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

        private void OnActionListUpdated(Message message)
        {
            if (ActionListReceived != null)
            {
                ActionListReceived(this, message);
            }
        }

        private void OnSettingsMessage(Message message)
        {
            if (SettingsReceived != null)
            {
                SettingsReceived(this, message);
            }
        }

        private void OnPercept(Message message)
        {
            if (PerceptReceived != null)
            {
                PerceptReceived(this, message);
            }
        }
        #endregion

    }
}
