using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TerraSim.ForestWorld.Actuators;
using TerraSim.ForestWorld.Entities;
using TerraSim.ForestWorld.Sensors;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld
{
    public class ForestWorldLogicProvider : ISimulationContentProvider
    {
        public struct MapFile
        {
            public string name;
            public string factoryFileName;
            public string weatherModelFileName;
            public string worldSettings;
            public List<string[]> tiles;
            public List<string[]> items;
        }

        private World world;
        private ServiceMediator mediator;
        private static string[] greens = new string[] { "Datura", "Fern", 
            "Narcissus", "Snowdrop" };
        private static string[] fungi = new string[] { "Amanita", "Galerina", 
            "RedPine", "Truffle", "White" };
    
        #region ISimulationLogicProvider Members

        public int MaxClients
        {
            get { return 3; }
        }

        public bool CanGenerateRandomMaps
        {
            get { return false; }
        }

        public World LoadMap(Stream inputStream, out string worldName, 
            WorldSettings? overrideWorldSettings = null)
        {
            if (world != null)
            {
                throw new Exception(
@"This instance is bound to a different World object. 
To get a new world, instantiante a new ContentProvider.");
            }
            Grid grid;
            GroundTileFactory factory;
            WorldSettings settings = WorldSettings.Default;
            MarkovModel weatherModel = null;
            List<string[]> items;
            using (StreamReader reader = new StreamReader(inputStream))
            {
                var map = JsonConvert.DeserializeObject<MapFile>(reader.ReadToEnd());
                worldName = map.name;
                LoadWorldSettings(overrideWorldSettings, ref settings, ref map);
                factory = LoadTileFactory(ref map);
                if (((overrideWorldSettings != null) && (overrideWorldSettings.Value.WeatherEnabled))
                    || settings.WeatherEnabled )
                {
                    LoadWeatherModel(ref weatherModel, ref map);
                }
                grid = Grid.FromTileList(map.tiles, factory, Grid.GridMode.Hexagonal);
                items = map.items;
            }
            world = new World(grid, 
                overrideWorldSettings ?? settings, 
                GenerateAgent, weatherModel, MaxClients);
            mediator = new ServiceMediator(world);
            var entityFactory = new ForestEntityFactory(mediator);
            foreach (var item in items)
                try
                {
                    var x = int.Parse(item[1]);
                    var y = int.Parse(item[2]);
                    switch (item[0])
                    {
                        case "Animal":
                            world.AddObject(entityFactory.CreateAnimal(),
                                x, y);
                            break;
                        case "Feeder":
                            world.AddObject(entityFactory.CreateFeeder(),
                                x, y);
                            break;
                        default:
                            if (fungi.Contains(item[0]))
                            {
                                world.AddObject(entityFactory.CreateFungus((Fungus.MushroomTypes)
                                    Enum.Parse(typeof(Fungus.MushroomTypes), item[0])),
                                    x, y);
                            }
                            if (greens.Contains(item[0]))
                            {
                                world.AddObject(entityFactory.CreateGreenPlant(
                                    (GreenPlant.GreenPlantTypes)
                                    Enum.Parse(typeof(GreenPlant.GreenPlantTypes), item[0])),
                                    x, y);
                            }
                            break;
                    }
                }
                catch (Exception)
                {
                }
            return world;
        }

        private static void LoadWeatherModel(ref MarkovModel weatherModel, ref MapFile map)
        {
            using (StreamReader wm = new StreamReader(
                new FileStream(map.weatherModelFileName, FileMode.Open, FileAccess.Read)))
            {
                var data = JsonConvert.DeserializeObject<MarkovModelStateData[]>(wm.ReadToEnd());
                weatherModel = new MarkovModel(data);
            }
        }

        private static GroundTileFactory LoadTileFactory(ref MapFile map)
        {
            GroundTileFactory factory;
            using (Stream tiles = new FileStream(map.factoryFileName,
                FileMode.Open, FileAccess.Read))
            {
                factory = new GroundTileFactory(tiles);
                factory.DispatcherGenerator = (tile)
                    => { return new GroundTileDispatchMediator(tile); };
            }
            return factory;
        }

        private static void LoadWorldSettings(WorldSettings? overrideWorldSettings, ref WorldSettings settings, ref MapFile map)
        {
            if (overrideWorldSettings == null)
            {
                using (StreamReader set = new StreamReader(map.worldSettings))
                {
                    settings = JsonConvert.DeserializeObject<WorldSettings>(set.ReadToEnd());
                }
            }
        }

        public World LoadMap(Stream inputStream, WorldSettings settings)
        {
            string str;
            return LoadMap(inputStream, out str, settings);
        }

        public World RandomMap(WorldSettings settings, MarkovModel weatherModel)
        {
            throw new NotImplementedException();
        }

        public Tuple<UserAgent, int, int> GenerateAgent(int agentId)
        {
            if (world == null)
            {
                throw new Exception("Instantiate a World first.");
            }
            var agent = new UserAgent(string.Format("agent_{0}", agentId), 
                agentId);
            agent.DispatchMediator = new AgentDispatchMediator(agent, mediator);
            agent.AddActuator(new Gun(agent, mediator));
            agent.AddActuator(new Wheels(agent, mediator));
            agent.AddActuator(new WaterTank(agent, mediator));
            agent.AddActuator(new PlantAnalyzer(agent, mediator));
            agent.AddActuator(new Arm(agent, mediator));
            agent.AddSensor(new BrightnessSensor(agent));
            agent.AddSensor(new Camera(agent));
            agent.AddSensor(new HumiditySensor(agent));
            agent.AddSensor(new Accumulator(agent, mediator));
            agent.AddSensor(new Clock(agent));
            agent.AddSensor(new Barometer(agent));
            return new Tuple<UserAgent, int, int>(agent, 1, 1);
        }

        #endregion
    }
}
