using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TerraSim.VacuumBot
{
    using EntityData = Dictionary<string, string>;
    using TerraSim.Network;
    public class VacuumBotClient
    {
        private NetworkClient network = null;

        public VacuumBotClient()
        {
            network = new NetworkClient();
            network.MessageParsed += OnMessageReceived;
        }

        public void Suck()
        {
            network.SendMessage("{ \"ActionName\": \"suck\" }", 
                MessageType.Command, MessageFormat.JSON);
        }

        public void Forward()
        {
            network.SendMessage("{ \"ActionName\": \"forward\" }", 
                MessageType.Command, MessageFormat.JSON);
        }

        public void TurnLeft()
        {
            network.SendMessage("{ \"ActionName\": \"left\" }", 
                MessageType.Command, MessageFormat.JSON);
        }

        public void TurnRight()
        {
            network.SendMessage("{ \"ActionName\": \"right\" }", 
                MessageType.Command, MessageFormat.JSON);
        }

        public void Connect(IPAddress address, int port)
        {
            network.Connect(address, port);
        }

        public void Disconnect()
        {
            network.Disconnect();
        }

        protected virtual void Percept(Dictionary<string, EntityData> data)
        {
        }

        private void OnMessageReceived(Message message)
        {
            var data = new Dictionary<string, EntityData>();
            EntityData entity;
            if ((message.Format == MessageFormat.JSON) 
                && (message.Type == MessageType.StateUpdate))
            {
                var o = JsonConvert.DeserializeObject<JObject>(message.Body);
                var e = o["has_attribute"].ToList();
                foreach (var en in e)
                {
                    if (!data.TryGetValue((string)en[0], out entity))
                    {
                        entity = new EntityData();
                        data[(string)en[0]] = entity;
                    }
                    data[(string)en[0]][(string)en[1]] = (string)en[2];
                }
            }
            Percept(data);
        }
    }
}
