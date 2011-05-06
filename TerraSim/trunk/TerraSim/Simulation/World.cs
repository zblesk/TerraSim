using System;
using System.Collections.Generic;
using TerraSim.Network;

namespace TerraSim.Simulation
{
    using AgentData = Tuple<int, DataCollection>;
    using CommandPair = Tuple<int, CommandMessage>;

    /// <summary>
    /// Maps to the input file with weather model transition matrix.
    /// </summary>
    internal enum WeatherChange
    {
        Rise = 0,
        Stay = 1,
        Fall = 2
    }

    public delegate Tuple<UserAgent, int, int> AgentGenerator(int agentId);

    public sealed class World
    {
        private Grid grid;
        private Dictionary<int, UserAgent> agents = new Dictionary<int, UserAgent>();
        private Dictionary<string, NamedObject> allEntities = new Dictionary<string, NamedObject>();
        private LinkedList<CommandPair> commandPool = new LinkedList<CommandPair>();
        private AgentGenerator GenerateAgent;
        Func<int, IntensityLevel> DaylightEvaluation = null;

        #region Environment
        private int timeOfDay;
        private int pressure;
        private WeatherChange change;
        /// <summary>
        /// Specifies at most how many pressure points can be changed in a single step
        /// of the weather model, when in the neutral state. 
        /// </summary>
        private const int maxPressureStepDelta = 7;
        /// <summary>
        /// Specifies at least how many pressure points can be changed in a single step
        /// of the weather model, when in the neutral state. 
        /// </summary>
        private const int minPressureStepDelta = 2;
        private const int maxPressure = 100;
        //Note: When Pressure is in the interval (rainThreshold, sunnyThreshold), it is cloudy.
        /// <summary>
        /// It rains if pressure is less than this threshold.
        /// </summary>
        private const int rainThreshold = 30;
        /// <summary>
        /// It is sunny when pressure is over this threshold.
        /// </summary>
        private const int sunnyThreshold = 60;
        private MarkovModel weatherModel;
        /// <summary>
        /// Indicates how many parts will a day be divided into.
        /// </summary>
        public readonly int DayPartCount = 100;
        /// <summary>
        /// The current level of (global) illumination in the world.
        /// </summary>
        public IntensityLevel LightLevel { get; set; }
        /// <summary>
        /// Gets the maximal number of clients that can be connected simultaneously.
        /// </summary>
        public int MaxClients { get; private set; }
        #endregion

        private Random rand = new Random();

        /// <summary>
        /// Current day
        /// </summary>
        public int Day { get; private set; }
        public WeatherType CurrentWeather { get; private set; }
        public Grid Map { get { return grid; } }
        public MessageBroadcaster MessageBroadcaster { get; private set; }

        public event EventHandler NewDay;
        public event Action BeforeUpdate;
        public event Action AfterUpdate;
       
        public World(Grid map, WorldSettings settings, AgentGenerator agentGenerator, 
            MarkovModel weatherModel = null, int maxClients = 1)
        {
            Settings = settings;
            this.DayPartCount = settings.DayPartCount;
            this.MaxClients = maxClients;
            TimeOfDay = Day = 0;
            pressure = settings.BarometricPressure;
            this.weatherModel = weatherModel;
            this.GenerateAgent = agentGenerator;
            grid = map;
            LightLevel = IntensityLevel.Full;
            int fadeStep = settings.LightFadeSpan / 5;
            DaylightEvaluation = (int i) =>
                {
                    if (i < settings.Dawn)
                    {
                        return IntensityLevel.VeryLow;
                    }
                    if (i < (settings.Dawn + settings.LightFadeSpan))
                    {
                        int shift = i - settings.Dawn;
                        return (IntensityLevel)(1 + shift / fadeStep);
                    }
                    if (i < settings.Dusk)
                    {
                        return IntensityLevel.Full;
                    }
                    if (i < (settings.Dusk + settings.LightFadeSpan))
                    {
                        int shift = i - settings.Dusk;
                        return (IntensityLevel)(5 - shift / fadeStep);
                    }
                    return IntensityLevel.VeryLow;
                };
            MessageBroadcaster = new MessageBroadcaster();
        }

        /// <summary>
        /// The day is divided to World.DayPartCount parts; 
        /// time is a number in [0, World.DayPartCount - 1].
        /// </summary>
        public int TimeOfDay
        {
            get { return timeOfDay; }
            private set { timeOfDay = value.Bound(0, DayPartCount - 1); }
        }
        public int Pressure 
        { 
            get { return pressure; } 
            private set { pressure = value.Bound(0, maxPressure); } 
        }
        public WorldSettings Settings { get; private set; }

        /// <summary>
        /// Adds the object to the world.
        /// </summary>
        /// <param name="obj">Object to be added</param>
        /// <param name="posX">X position on the grid</param>
        /// <param name="posY">Y position on the grid</param>
        /// <remarks>Do not use to add an agent. Use AddAgent(.) instead.</remarks>
        public void AddObject(NamedObject obj, int posX, int posY)
        {
            if (obj is UserAgent)
            {
                throw new Exception("Do not use this method do add an agent.");
            }
            AddEntity(obj, posX, posY);
        }

        /// <summary>
        /// Creates and adds an new agent instance into the simulation.
        /// </summary>
        /// <param name="agentId">ID of the new agent.</param>
        public void AddAgent(int agentId)
        {
            var tuple = GenerateAgent(agentId);
            AddEntity(tuple.Item1, tuple.Item2, tuple.Item3);
            agents[tuple.Item1.Id] = tuple.Item1;
        }
        
        /// <summary>
        /// Moves an object in the simulation. Does not check for any constraints.
        /// </summary>
        /// <param name="obj">Object to be moved</param>
        /// <param name="posX">X coordinate of the new position</param>
        /// <param name="posY">Y coordinate of the new position</param>
        public void MoveObject(NamedObject obj, int posX, int posY)
        {
            grid[obj.PositionX, obj.PositionY].Objects.Remove(obj);
            grid[posX, posY].Objects.Add(obj);
            obj.PositionY = posY;
            obj.PositionX = posX;
        }
        
        public void Update()
        {
            OnBeforeUpdate();
            NextTimeUnit();
            var args = new WorldUpdateEventArgs() { World = this };
            ExecutePooledCommands(args);
            foreach (UserAgent agent in agents.Values)
            {
                agent.Update(args);
            }
            foreach (Hex hex in grid)
            {
                foreach (var obj in hex)
                {
                    if (!(obj is UserAgent))
                    {
                        obj.Update(args);
                    }
                }
            }
            UpdateWeather();
            UpdateDaylight();
            foreach (var agent in agents.Values)
            {
                agent.UpdateSensors(args);
            }
            OnAfterUpdate();
        }

        internal void EnqueueAgentCommand(int id, CommandMessage command)
        {
            //Add this command to the pool. It will be executed on the nearest update.
            commandPool.AddLast(new CommandPair(id, command));
        }

        /// <summary>
        /// Increases the TimeOfDay.
        /// </summary>
        /// <returns>Returns the new time value.</returns>
        internal int NextTimeUnit()
        {
            if (timeOfDay == (DayPartCount - 1))
            {
                timeOfDay = 0;
                OnNewDay();
                return 0;
            }
            return ++timeOfDay;
        }

        internal void RemoveObject(NamedObject obj)
        {
            grid[obj.PositionX, obj.PositionY].Objects.Remove(obj);
            allEntities.Remove(obj.Name);
        }

        /// <summary>
        /// Gets all the relevant data for an agent.
        /// </summary>
        public DataCollection GetAgentData(int id)
        {
            var result = new DataCollection();
            UserAgent agent = GetAgent(id);
            agent.Marshal(result);
            return result;
        }

        /// <summary>
        /// Gets a list of commands the agent can execute.
        /// </summary>
        /// <param name="id">ID of the agent,</param>
        /// <returns>Returns a sequence of names the agent is capable 
        /// of performing.</returns>
        public IEnumerable<string> GetAgentCapabilities(int id)
        {
            return GetAgent(id).GetPossibleActions();
        }

        /// <summary>
        /// Gets an object by name.
        /// </summary>
        /// <param name="name">Name of the object to get.</param>
        /// <returns>Returns the found object, or null if an invalid 
        /// name was specified.</returns>
        public NamedObject GetObjectByName(string name)
        {
            NamedObject result;
            if (allEntities.TryGetValue(name, out result))
            {
                return result;
            }
            return null;
        }

        private void ExecutePooledCommands(WorldUpdateEventArgs args)
        {
            UserAgent agent;
            foreach (var cmd in commandPool)
            {
                if (!agents.TryGetValue(cmd.Item1, out agent))
                {
                    throw new Exception("Internal inconsistency: agent not found.");
                }
                agent.PerformAction(cmd.Item2.ActionName, cmd.Item2.Arg1, 
                    cmd.Item2.Arg2, args);
            }
            commandPool.Clear();
        }
        
        private void UpdateWeather()
        {
            if (!Settings.WeatherEnabled)
            {
                return;
            }
            change = (WeatherChange)weatherModel.NextState();
            switch (change)
            {
                case WeatherChange.Rise:
                    Pressure += rand.Next(minPressureStepDelta * 2, maxPressureStepDelta * 2);
                    break;
                case WeatherChange.Stay:
                    Pressure += (rand.Next(2) == 0 ? -1 : 1)
                        * rand.Next(minPressureStepDelta, maxPressureStepDelta);
                    break;
                case WeatherChange.Fall:
                    Pressure -= rand.Next(minPressureStepDelta * 2, maxPressureStepDelta * 2);
                    break;
                default:
                    throw new Exception("Should not happen. Unknown 'weather change' value.");
            }
            if (Pressure <= rainThreshold)
            {
                CurrentWeather = WeatherType.Rainy;
            }
            else if (Pressure < sunnyThreshold)
            {
                CurrentWeather = WeatherType.Cloudy;
            }
            else
            {
                CurrentWeather = WeatherType.Sunny;
            }
        }

        private void UpdateDaylight()
        {
            //DaylightEvaluation returns the current light level based on time. 
            //It is then decreased according to weather (by 0 if it is Sunny, 
            //by 2 if it is rainy.)
            LightLevel = (IntensityLevel)(((int)DaylightEvaluation(TimeOfDay)) 
                - ((int)CurrentWeather)).Bound(1, 5);
        }
        
        /// <summary>
        /// Adds the object to the world.
        /// </summary>
        /// <param name="obj">Object to be added</param>
        /// <param name="posX">X position on the grid</param>
        /// <param name="posY">Y position on the grid</param>
        private void AddEntity(NamedObject obj, int posX, int posY)
        {
            grid[posX, posY].Objects.Add(obj);
            obj.PositionX = posX;
            obj.PositionY = posY;
            allEntities[obj.Name] = obj;
        }

        private UserAgent GetAgent(int id)
        {
            UserAgent agent;
            if (!agents.TryGetValue(id, out agent))
            {
                throw new Exception("Internal inconsistency: agent not found.");
            }
            return agent;
        }

        #region Event triggers

        private void OnNewDay()
        {
            Day++;
            if (NewDay != null)
            {
                NewDay(this, EventArgs.Empty);
            }
        }

        private void OnBeforeUpdate()
        {
            if (BeforeUpdate != null)
            {
                BeforeUpdate();
            }
        }

        private void OnAfterUpdate()
        {
            if (AfterUpdate != null)
            {
                AfterUpdate();
            }
        }

        #endregion

        #region IEnumerable implementation
        public IEnumerable<AgentData> GetAllAgentData()
        {
            foreach (int agentId in agents.Keys)
            {
                yield return new AgentData(agentId, GetAgentData(agentId));
            }
        }
        #endregion

    }

    public class WorldUpdateEventArgs : EventArgs
    {
        public World World;
    }

}
