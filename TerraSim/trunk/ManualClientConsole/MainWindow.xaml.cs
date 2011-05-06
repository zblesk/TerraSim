using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TerraSim;
using TerraSim.ManualClient;
using TerraSim.Simulation;

namespace ManualClientConsole
{
    using EntityData = Dictionary<string, string>;
    //A node contains a TreViewItem and its entity data.
    using Node = Tuple<TreeViewItem, Dictionary<string, string>>;
    using TerraSim.Network; 

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ManualClient client = new ManualClient();
        Dictionary<string, Node> entities =
            new Dictionary<string, Node>();
        MemoryStream stringStream = new MemoryStream();
        JsonTextWriter writer = null;
        StreamReader reader = null;

        public MainWindow()
        {
            InitializeComponent();
            writer = new JsonTextWriter(new StreamWriter(stringStream));
            reader = new StreamReader(stringStream);
            TraceListener listener = new TextBlockTraceListener(tbTrace);
            listener.TraceOutputOptions = TraceOptions.DateTime;
            Trace.Listeners.Add(listener);
            client.Connected += (s, a)
                => { btnConnect.Content = "Disconnect"; AddToLog("Connected."); };
            client.Disconnected += (s, a)
                => { btnConnect.Content = "Connect"; AddToLog("Disconnected."); };
            client.ActionListReceived += OnActionListUpdate;
            client.SettingsReceived += OnSettingsRecieved;
            client.PerceptReceived += OnPerceptReceived;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (!client.IsConnected)
            {
                int port;
                if (!int.TryParse(tbPort.Text, out port))
                {
                    port = SimulationCore.DefaultPort;
                }
                AddToLog("Trying to connect.");
                tvEntities.Items.Clear();
                cbCommand.Items.Clear();
                client.Connect(IPAddress.Parse(tbIP.Text), port);
            }
            else
            {
                client.Disconnect();
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            stringStream.SetLength(0);
            writer.WriteStartObject();
            writer.WritePropertyName("ActionName");
            writer.WriteValue(cbCommand.SelectedValue);
            writer.WritePropertyName("Arg1");
            writer.WriteValue(tbArg1.Text);
            writer.WritePropertyName("Arg2");
            writer.WriteValue(tbArg2.Text);
            writer.WriteEndObject();
            writer.Flush();
            stringStream.Position = 0;
            client.SendMessage(reader.ReadToEnd(), MessageType.Command, MessageFormat.JSON);
            AddToLog("Command sent: {0}({1}, {2})".Form(cbCommand.SelectedValue,
                tbArg1.Text, tbArg2.Text));
        }

        public void OnActionListUpdate(ManualClient sender, Message message)
        {
            var items = from it in JsonConvert.DeserializeObject<string[]>(message.Body)
                        select it;
            cbCommand.Dispatcher.Invoke(new Action(() =>
                {
                    cbCommand.Items.Clear();
                    foreach (var it in items)
                    {
                        cbCommand.Items.Add(it);
                    }
                    AddToLog("Action list arrived.");
                }));
        }

        public void OnSettingsRecieved(ManualClient sender, Message message)
        {
            cbCommand.Dispatcher.Invoke(new Action(() =>
            {
                AddToLog("Settings arrived.");
                AddToLog("Message contained: ");
                AddToLog(message.Body);
            }));
        }

        public void OnPerceptReceived(ManualClient sender, Message message)
        {
            tvEntities.Dispatcher.Invoke(new Action(() =>
            {
                if ((bool)cbClearWindow.IsChecked)
                {
                    tvEntities.Items.Clear();
                    entities.Clear();
                }
            }));
            var o = JsonConvert.DeserializeObject<JObject>(message.Body);
            var e = o["has_attribute"].ToList();
            tvEntities.Dispatcher.BeginInvoke(new Action(() => { DisplayAttributes(ref message, e); }));
            try
            {
                tbOngoingActions.Dispatcher.BeginInvoke((Action)CleanOngoingActions);
                var acts = o["performs_action"].ToList();
                tbOngoingActions.Dispatcher.BeginInvoke(new Action(() => { DisplayActions(acts); }));
            }
            catch (ArgumentException)
            {
                // do nothing - "performs_action" was empty.
            }
        }

        private void DisplayActions(List<JToken> acts)
        {
            string lines = string.Empty;
            foreach (var act in acts)
            {
                lines += "{0} performs {1} with parameters ({2}, {3})\n"
                    .Form(act[0], act[1], act[2], act[3]);
            }
            tbOngoingActions.Text = lines;
        }

        private void DisplayAttributes(ref Message message, List<JToken> e)
        {
            Node entity;
            string name, property, value, newValue;
            EntityData data;
            TreeViewItem currentEntity;
            AddToLog("Percept received. Message body length: {0}."
                .Form(message.Body.Length));
            foreach (var en in e)
            {
                name = (string)en[0];
                if (!entities.TryGetValue(name, out entity))
                {
                    entity = new Node(new TreeViewItem(), new EntityData());
                    entities[name] = entity;
                    entity.Item1.Header = name;
                    tvEntities.Items.Add(entity.Item1);
                }
                currentEntity = entity.Item1;
                data = entity.Item2;
                property = (string)en[1];
                value = (string)en[2];
                if (data.ContainsKey(property))
                {
                    currentEntity.Items.Remove(data[property]);
                    data.Remove(property);
                }
                newValue = "{0} = {1}".Form(property, value);
                data[property] = newValue;
                currentEntity.Items.Add(newValue);
            }
        }

        private void CleanOngoingActions()
        {
            tbOngoingActions.Text = string.Empty;
        }

        private void AddToLog(string str)
        {
            tbLog.Text += str + '\n';
        }
        
    }
}
