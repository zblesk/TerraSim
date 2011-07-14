using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using TerraSim.Network;

namespace TerraSim.Simulation
{
    public class SimulationCore
    {
        private enum RequestedDataType { Stats, Capabilities }

        private Stopwatch timer;
        private NetworkServer networkCore = null;
        private World world;
        private DateTime started;
        private TimedEventQueue timeQueue = new TimedEventQueue();
        private Queue<Tuple<int, SimulationCore.RequestedDataType>> dataRequestQueue
            = new Queue<Tuple<int, RequestedDataType>>();
        private bool updateRunning = false;
        /// <summary>
        /// When was the last time the day part changed.
        /// </summary>
        private long lastTimeChange;
        /// <summary>
        /// Duration of one part of a day in miliseconds.
        /// </summary>
        private long timeUnitDuration;
        /// <summary>
        /// Indicates when the last update was executed.
        /// </summary>
        private long lastUpdate;
        private const int second = 1000;
        private const int minute = 60 * second;
        private const int minimumRequiredTimeUnitDuration = 100; //in miliseconds
        private const string tellCommandString = "tell";

        public ServerSettings Settings { get; private set; }

        public event EventHandler Started;
        public event EventHandler Stopped;
        /// <summary>
        /// The default port for the network server.
        /// </summary>
        public const int DefaultPort = NetworkServer.DefaultPort; 
        /// <summary>
        /// True if the simulation is currently running. Read-only.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// How long since the server was started?
        /// </summary>
        public TimeSpan Uptime
        {
            get
            {
                if (!IsRunning)
                {
                    return TimeSpan.Zero;
                }
                return started - DateTime.Now;
            }
        }

        public SimulationCore()
        {
            //Not needed for now; handles regular timer cleanup.
            //timeQueue.Enqueue(() => { timeQueue.Purge(); }, 5 * minute, true);
        }

        /// <summary>
        /// Starts the simulation.
        /// </summary>
        public void Start(ServerSettings settings, World world)
        {
            started = DateTime.Now;
            timer = new Stopwatch();
            lastTimeChange = 0;
            IsRunning = true;
            lastUpdate = 0;
            networkCore = new NetworkServer(settings.NetworkPort == -1 
                ? NetworkServer.DefaultPort : settings.NetworkPort);
            this.world = world;
            timeUnitDuration = settings.DayCycleLength * minute / world.DayPartCount;
            if (timeUnitDuration < minimumRequiredTimeUnitDuration)
            {
                throw new Exception(@"One time unit would only take {0} miliseconds. 
The required minimum is {1}. You can remedy this e.g. by increasing
the day duration. (Currently {2} seconds.)"
                    .Form(timeUnitDuration, minimumRequiredTimeUnitDuration,
                        settings.DayCycleLength));
            }
            Settings = settings;
            world.AfterUpdate += WorldUpdated;
            timer.Start();
            EnqueueEvent(UpdateWorld, timeUnitDuration, true);
            timeQueue.Start();
            networkCore.Start();
            networkCore.MessageReceived += HandleNewMessage;
            networkCore.ClientConnected += HandleNewClient;
            Trace.TraceInformation("Simulation core initialized and started.");
        }

        /// <summary>
        /// Stops the simulation.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
            {
                Trace.TraceWarning(
                    "Simulation core is being stopped, though it was never started.");
                return;
            }
            IsRunning = false;
            timer.Stop();
            networkCore.Stop();
            timeQueue.Destroy();
            networkCore = null;
            timer = null;
            timeQueue = null;
        }
        
        /// <summary>
        /// Adds an event to be executed with a delay
        /// </summary>
        /// <param name="callback">A function to be called</param>
        /// <param name="delay">Delay in miliseconds</param>
        /// <param name="autoReset">True if the event should repeat</param>
        public void EnqueueEvent(Action callback,
            double delay, bool autoReset = false)
        {
            timeQueue.Enqueue(callback, delay, autoReset);
        }
                
        /// <summary>
        /// Update the simulation world.
        /// </summary>
        private void Update()
        {
            long delta = timer.ElapsedMilliseconds - lastUpdate;
            lastUpdate = timer.ElapsedMilliseconds; //never mind the possible slight change
            if ((lastUpdate - lastTimeChange) >= timeUnitDuration)
            {
                world.NextTimeUnit();
            }
        }

        private void UpdateWorld()
        {
            if (updateRunning)
            {
                return;
            }
            updateRunning = true;
            world.Update();
        }

        private void WorldUpdated()
        {
            if (!IsRunning)
            {
                return;
            }
            while (dataRequestQueue.Count != 0)
            {
                var t = dataRequestQueue.Dequeue();
                switch (t.Item2)
                {
                    case RequestedDataType.Capabilities:
                        if (!networkCore.IsClientConnected(t.Item1))
                        {
                            break;
                        }
                        var jsonMessage = JsonConvert.SerializeObject(
                            world.GetAgentCapabilities(t.Item1));
                        networkCore.SendMessage(t.Item1,
                            jsonMessage, MessageType.Capabilities, MessageFormat.JSON);
                        break;
                    default:
                        Trace.TraceError("Unknown requested data type.");
                        break;
                }
            }
            foreach (var data in world.GetAllAgentData())
            {
                // Optional: not only JSON.
                if (networkCore.IsClientConnected(data.Item1))
                {
                    networkCore.SendMessage(data.Item1,
                        Marshallers.ToJSON(data.Item2),
                        MessageType.StateUpdate,
                        MessageFormat.JSON);
                }
            }
            updateRunning = false;
        }

        private void HandleNewMessage(int clientId, Message message)
        {
            if (message.Format == MessageFormat.Predicate)
            {
                throw new Exception("Unsupported message format.");
            }
            if (message.Type == MessageType.Command)
            {
                CommandMessage c = JsonConvert.
                    DeserializeObject<CommandMessage>(message.Body);
                try
                {
                    c.ActionName = c.ActionName.ToLowerInvariant();
                    if (c.ActionName == tellCommandString) //the special case of Tell action
                    {
                        int targetAgentId = (world.GetObjectByName(c.Arg1) as UserAgent).Id;                        
                        world.EnqueueAgentCommand(targetAgentId, c);
                    }
                    else
                    {
                        world.EnqueueAgentCommand(clientId, c);
                    }
                }
                catch (NullReferenceException) { }
            }
        }

        private void HandleNewClient(object sender, int clientId)
        {
            if (networkCore.ClientCount < world.MaxClients)
            {
                world.AddAgent(clientId);
                dataRequestQueue.Enqueue(
                    new Tuple<int, RequestedDataType>(clientId, RequestedDataType.Capabilities));
            }
            else
            {
                networkCore.KickClient(clientId);
            }
        }

        #region Event Handlers

        private void OnStarted()
        {
            if (Started != null)
            {
                Started(this, EventArgs.Empty);
            }
        }

        private void OnStopped()
        {
            if (Stopped != null)
            {
                Stopped(this, EventArgs.Empty);
            }
        }

        #endregion

    }
}
