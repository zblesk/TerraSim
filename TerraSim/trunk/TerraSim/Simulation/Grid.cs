using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TerraSim.Simulation
{
    using Coords = Tuple<int, int>;

    public class Grid : IEnumerable<Hex>
    {
        public enum GridMode
        {
            Hexagonal,
            Octagonal
        }
        
        private Hex[] grid { get; set; }
        private Func<int, int, IEnumerable<Coords>> NeighbourhoodEnumerationFunction = null;
        private Func<int, int, int, int, int> DistanceFunction = null;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public Hex this[int x, int y]
        {
            get { return grid[x * Width + y]; } //Equivalent to calling TransformCoordinates(x, y). We spare 1 call.
            set { grid[x * Width + y] = value; }
        }

        /// <summary>
        /// Load a rectangunar map from a JSON stream.
        /// </summary>
        /// <param name="jsonData">The input stream, containing well-formed JSON data.</param>
        /// <param name="tileFactory">A factory for GroundTile creation.</param>
        /// <returns>Returns a map.</returns>
        public static Grid FromStream(Stream jsonData, GroundTileFactory tileFactory,
            GridMode gridMode = GridMode.Octagonal)
        {
            var reader = new StreamReader(jsonData);
            var list = JsonConvert.DeserializeObject<List<string[]>>(reader.ReadToEnd());
            int lines = list.Count;
            if (lines < 1)
            {
                throw new ArgumentException("Input map must contain some data. 0 rows found.",
                    "jsonData");
            }
            int width = list[0].Length;
            if (width < 1)
            {
                throw new ArgumentException("Input map must contain some data. 0 columns found.",
                    "jsonData");
            }
            Grid result = new Grid(width, lines, gridMode);
            return FillGrid(tileFactory, list, width, result);
        }

        public static Grid FromTileList(List<string[]> tiles, GroundTileFactory tileFactory,
            GridMode gridMode = GridMode.Octagonal)
        {
            Grid result = new Grid(tiles.Count, tiles[0].Length, gridMode);
            return FillGrid(tileFactory, tiles, tiles[0].Length, result);
        }

        /// <summary>
        /// Creates a new HexGrid.
        /// </summary>
        /// <param name="height">Height of the grid</param>
        /// <param name="width">Width of the grid</param>
        /// <param name="init">If true, all fields will be initialized
        /// with a new, empty Hex. If false, all fields will be null.</param>
        /// <param name="mode">Mode of the grid.</param>
        public Grid(int height, int width, 
            GridMode mode = GridMode.Octagonal, bool init = false)
        {
            Width = width;
            Height = height;
            grid = new Hex[width * height];
            NeighbourhoodEnumerationFunction = (mode == GridMode.Octagonal)
                ? (Func<int, int, IEnumerable<Coords>>) EnumerateOctNeighbourCoords 
                : EnumerateHexNeighbourCoords;
            DistanceFunction = (mode == GridMode.Octagonal)
                ? (Func<int, int, int, int, int>)OctGridDistance
                : HexGridDistance;
            if (init)
            {
                for (int i = 0; i < grid.Length; i++)
                {
                    grid[i] = new Hex();
                }
            }
        }
        
        public HashSet<Hex> EnumerateNeighbours(int x, int y, int distance)
        {
            if (!Contains(x, y) || (distance < 1))
            {
                return new HashSet<Hex>();
            }
            var visited = new Dictionary<Coords, int>();
            var initialCoords = new Coords(x, y);
            visited.Add(initialCoords, distance);
            var queue = new Queue<Coords>();
            queue.Enqueue(initialCoords);
            int newDist = 0;
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                newDist = visited[item] - 1;
                if (newDist < 0)
                {
                    continue;
                }
                foreach (var n in NeighbourhoodEnumerationFunction(item.Item1, item.Item2))
                {
                    if (!visited.ContainsKey(n))
                    {
                        visited[n] = newDist;
                        queue.Enqueue(n);
                    }
                }
            }
            visited.Remove(initialCoords);
            return new HashSet<Hex>(from vis in visited.Keys 
                                    select this[vis.Item1, vis.Item2]);
        }

        /// <summary>
        /// Tries to get a hex at given coordinates.
        /// </summary>
        /// <returns>Teturns the given Hex, or null if the specified position was invalid/out of the grid.</returns>
        public bool TryGet(int x, int y, out Hex hex)
        {
            hex = null;
            if (!Contains(x, y))
            {
                return false; 
            }
            hex = this[x, y];
            return true;
        }

        /// <summary>
        /// Determines whether the grid contains a hex at given position.
        /// </summary>
        /// <param name="x">X coordinate of the hex.</param>
        /// <param name="y">Y coordinate of the hex.</param>
        /// <returns>Returns true if there is a hex at given coordinates, 
        /// otherwise false.</returns>
        public bool Contains(int x, int y)
        {
            return !((x < 0) || (x >= Height) || (y < 0) || (y >= Width));
        }
        
        /// <summary>
        /// Determines the distance between two tiles given by coordinates.
        /// </summary>
        /// <param name="x1">X coord of the first object</param>
        /// <param name="y1">Y coord of the first object</param>
        /// <param name="x2">X coord of the second object</param>
        /// <param name="y2">Y coord of the second object</param>
        /// <returns></returns>
        public int Distance(int x1, int y1, int x2, int y2)
        {
            return DistanceFunction(x1, y1, x2, y2);
        }

        private static Grid FillGrid(GroundTileFactory tileFactory,
            List<string[]> list, int width, Grid result)
        {
            int i = 0;
            GroundTile t;
            foreach (string[] l in list)
            {
                for (int c = 0; c < width; c++)
                {
                    t = tileFactory.CreateTile(l[c]);
                    t.PositionX = i;
                    t.PositionY = c;
                    result[i, c] = new Hex(t);
                }
                ++i;
            }
            return result;
        }

        private int TransformCoordinates(int x, int y)
        {
            return x * Width + y;
        }

        private HashSet<Hex> EnumerateHexNeighbours(int x, int y)
        {
            var neigh = EnumerateHexNeighbourCoords(x, y);
            return (neigh == null) ? null : new HashSet<Hex>(
                from coord in neigh
                select this[coord.Item1, coord.Item2]);
        }

        private IEnumerable<Coords> EnumerateHexNeighbourCoords(int x, int y)
        {
            if (!Contains(x, y))
            {
                return null;
            }
            var result = new List<Coords>();
            int offset = (x % 2) == 0 ? -1 : 1;
            Hex hex;

            if (TryGet(x - 1, y, out hex))
            {
                result.Add(new Coords(x - 1, y));
            }
            if (TryGet(x - 1, y + offset, out hex))
            {
                result.Add(new Coords(x - 1, y + offset));
            }
            if (TryGet(x, y - 1, out hex))
            {
                result.Add(new Coords(x, y - 1));
            }
            if (TryGet(x + 1, y, out hex))
            {
                result.Add(new Coords(x + 1, y));
            }
            if (TryGet(x + 1, y + offset, out hex))
            {
                result.Add(new Coords(x + 1, y + offset));
            }
            if (TryGet(x, y + 1, out hex))
            {
                result.Add(new Coords(x, y + 1));
            }
            return result;
        }

        private HashSet<Hex> EnumerateOctNeighbours(int x, int y)
        {
            var neigh = EnumerateOctNeighbourCoords(x, y);
            return (neigh == null) ? null : new HashSet<Hex>(
                from coord in neigh
                select this[coord.Item1, coord.Item2]);
        }

        private IEnumerable<Coords> EnumerateOctNeighbourCoords(int x, int y)
        {
            if (!Contains(x, y))
            {
                return null;
            }
            Hex hex;
            return from i in Enumerable.Range(-1, 3)
                   from j in Enumerable.Range(-1, 3)
                   where (i != 0 || j != 0)
                       && TryGet(x + i, y + j, out hex)
                   select new Coords(x + i, y + j);
        }

        /// <summary>
        /// Determines the distance between two tiles 
        /// on a octagonal grid.
        /// </summary>
        /// <param name="x1">X coord of the first object</param>
        /// <param name="y1">Y coord of the first object</param>
        /// <param name="x2">X coord of the second object</param>
        /// <param name="y2">Y coord of the second object</param>
        /// <returns></returns>
        private int OctGridDistance(int x1, int y1, int x2, int y2)
        {
            int dx = Math.Abs(x1 - x2);
            int dy = Math.Abs(y1 - y2);
            return Math.Max(dx, dy);
        }

        /// <summary>
        /// Determines the distance between two tiles 
        /// on a hexagonal grid.
        /// </summary>
        /// <param name="x1">X coord of the first object</param>
        /// <param name="y1">Y coord of the first object</param>
        /// <param name="x2">X coord of the second object</param>
        /// <param name="y2">Y coord of the second object</param>
        /// <returns></returns>
        private int HexGridDistance(int x1, int y1, int x2, int y2)
        {
            double x1d, x2d, y1d, y2d;
            x1d = Math.Round(0.9 * x1);
            y1d = Math.Round(0.5 * x1 + y1);
            x2d = Math.Round(0.9 * x2);
            y2d = Math.Round(0.5 * x2 + y2);
            return (int)Math.Max(Math.Abs(x1d - x2d), Math.Abs(y1d - y2d));
        }


        #region IEnumerable<Hex> Members

        /// <summary>
        /// Enumerates the Hex grid members.
        /// </summary>
        /// <returns>Returns an enumerator of Hex items.</returns>
        public IEnumerator<Hex> GetEnumerator()
        {
            return grid.Cast<Hex>().GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return grid.GetEnumerator();
        }

        #endregion
    }
}
