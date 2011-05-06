using System;
using System.Collections.Generic;
using System.Linq;
using TerraSim.VacuumBot;

namespace VacuumBot
{
    using Entity = Dictionary<string, string>;

    class MyClient : VacuumBotClient
    {
        private enum Tiles { Unknown, Wall, Dirt, Clean }

        private Tiles[,] map = new Tiles[200, 200];
        private int posX = 0, posY = 0;
        Random rand = new Random();        

        public event Action<int, int> PositionSeen;
   
        protected override void Percept(Dictionary<string, Entity> data)
        {
            ReportSeenTiles(data);
            UpdateMap(data);
            if (map[posX, posY] == Tiles.Dirt)
            {
                Suck();
            }
            else
                switch (rand.Next(4))
                {
                    case (0): Forward(); break;
                    case (1): TurnLeft(); break;
                    case (2): TurnRight(); break;
                    case (3): Forward(); Forward(); break;
                }
        }

        private void ReportSeenTiles(Dictionary<string, Entity> data)
        {
            //select all entities with the "position" attribute.
            if (PositionSeen != null)
            {
                var s = (from val in data.Values
                         where (from v in val.Values
                                where v.StartsWith("position")
                                select 1).Count() > 0
                         select val).ToArray();
                foreach (var j in data)
                {
                    var i = j.Value;
                    PositionSeen(int.Parse(i["position_x"]), int.Parse(i["position_y"]));
                }
            }
        }

        private void UpdateMap(Dictionary<string, Entity> data)
        {
            var ground = from val in data.Values
                         where val["type"] == "clean" || val["type"] == "wall"
                         select val;
            var agent = from val in data.Values
                        where val["type"] == "agent"
                        select val;
            var dirt = from val in data.Values
                       where val["type"] == "dirt"
                       select val;
            var agent_enum = agent.GetEnumerator();
            if (agent_enum.MoveNext())
            {
                posX = int.Parse(agent_enum.Current["position_x"]);
                posY = int.Parse(agent_enum.Current["position_y"]);
            }
            agent_enum.Dispose();
            foreach (var tile in ground)
            {
                map[int.Parse(tile["position_x"]), int.Parse(tile["position_y"])] =
                    (Tiles)Enum.Parse(typeof(Tiles), tile["type"], true);
            }
            foreach (var tile in dirt)
            {
                map[int.Parse(tile["position_x"]), int.Parse(tile["position_y"])] = Tiles.Dirt;
            }
        }
    }
}
