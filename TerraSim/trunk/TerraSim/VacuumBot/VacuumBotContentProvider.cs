using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TerraSim.Simulation;

namespace TerraSim.VacuumBot
{
    public sealed class VacuumBotContentProvider : ISimulationContentProvider
    {
        GroundTileFactory factory = null;

        public VacuumBotContentProvider()
        {
            using (Stream re = new FileStream("tiledefs.json", FileMode.Open, FileAccess.Read))
            {
                factory = new GroundTileFactory(re);
            }
        }

        #region ISimulationLogicProvider Members

        public int MaxClients { get { return 5; } }
        public bool CanGenerateRandomMaps { get { return false; } }

        public World LoadMap(Stream mapInputStream, WorldSettings settings)
        {
            Grid grid = Grid.FromStream(mapInputStream, factory);
            using (Stream jsonData = new FileStream("vacuum_map_items.json", 
                FileMode.Open, FileAccess.Read))
            {
                var reader = new StreamReader(jsonData);
                var list = JsonConvert.DeserializeObject<List<string[]>>(reader.ReadToEnd());
                int lines = list.Count;
                int width = list[0].Length;
                int i = 0;
                foreach (string[] l in list)
                {
                    for (int c = 0; c < width; c++)
                    {
                        if (l[c].Length != 0)
                        {
                            var o = new InanimateObject(l[c] + '_' + i.ToString() + c.ToString());
                            o.Type = l[c];
                            o.PositionX = i;
                            o.PositionY = c;
                            grid[i, c].Objects.Add(o);
                        }
                    }
                    ++i;
                }
            }
            return new World(grid, settings, GenerateAgent);
        }

        public World RandomMap(WorldSettings settings, MarkovModel weatherModel)
        {
            throw new NotImplementedException();
        }

        public Tuple<UserAgent, int, int> GenerateAgent(int agentId)
        {
            var agent = new UserAgent(string.Format("agent_{0}", agentId), agentId);
            var vv = new TerraSim.VacuumBot.Vacuum(agent);
            agent.AddActuator(vv);
            agent.AddActuator(new TerraSim.VacuumBot.Motor(agent));
            agent.AddSensor(new TerraSim.VacuumBot.Camera(agent));
            return new Tuple<UserAgent, int, int>(agent, 1, 1);
        }

        #endregion
    }
}
