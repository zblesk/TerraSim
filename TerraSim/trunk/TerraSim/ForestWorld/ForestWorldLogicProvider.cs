using System;
using System.Collections.Generic;
using System.IO;
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
            using (StreamReader reader = new StreamReader(inputStream))
            {
                var map = JsonConvert.DeserializeObject<MapFile>(reader.ReadToEnd());
                worldName = map.name;
                if (overrideWorldSettings == null)
                {
                    using (StreamReader set = new StreamReader(map.worldSettings))
                    {
                        settings = JsonConvert.DeserializeObject<WorldSettings>(set.ReadToEnd());
                    }
                }
                using (Stream tiles = new FileStream(map.factoryFileName,
                    FileMode.Open, FileAccess.Read))
                {
                    factory = new GroundTileFactory(tiles);
                    factory.DispatcherGenerator = (tile)
                        => { return new GroundTileDispatchMediator(tile); };
                }
                if (((overrideWorldSettings != null) && (overrideWorldSettings.Value.WeatherEnabled))
                    || settings.WeatherEnabled )
                {
                    using (StreamReader wm = new StreamReader(
                        new FileStream(map.weatherModelFileName, FileMode.Open, FileAccess.Read)))
                    {
                        var data = JsonConvert.DeserializeObject<MarkovModelStateData[]>(wm.ReadToEnd());
                        weatherModel = new MarkovModel(data);
                    }
                }
                grid = Grid.FromTileList(map.tiles, factory, Grid.GridMode.Hexagonal);
            }
            world = new World(grid, 
                overrideWorldSettings ?? settings, 
                GenerateAgent, weatherModel, MaxClients);
            mediator = new ServiceMediator(world);
            world.AddObject(new Animal("an1", mediator), 1, 2);
            world.AddObject(new Fungus("f1", Fungus.MushroomTypes.Amanita), 1, 2);
            world.AddObject(new GreenPlant("p1", GreenPlant.GreenPlantTypes.Fern), 1, 2);
            world.AddObject(new GreenPlant("n1", GreenPlant.GreenPlantTypes.Narcissus), 2, 2);
            world.AddObject(new AnimalFeeder("feeder1"), 3, 5);
            return world;
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
