using System;
using System.Collections.Generic;
using System.Linq;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Entities
{
    /// <summary>
    /// A fungus. Without chlorophyll, may or may not be poisonous. Is hydrophobic. 
    /// If it reaches its maximum Size and the ground is wet enough, 
    /// it may spread (new funghi of the same type will spawn in the vicinity).
    /// </summary>
    public class Fungus : Plant
    {
        public enum MushroomTypes { White, Truffle, RedPine, Amanita, Galerina }

        internal class Mediator : PlantMediator
        {
            private Fungus Owner { get { return owner as Fungus;}}

            public Mediator(Fungus owner)
                : base(owner)
            {
            }

            internal void WasWatered()
            {
                Owner.Size++; 
            }

            internal void WasAnalyzed()
            {
                analyzed[Owner.mushroomType] = true;
            }
        }

        private static Random rand = new Random();
        private static Dictionary<MushroomTypes, bool> analyzed;
        private const int spawnChance = 35;
        private const int growChance = 5;
        private int maxSize;
        private int size = 1;
        private MushroomTypes mushroomType;
        private readonly bool isPoisonous = false;
        private int spawned_count = 0;
        protected override bool IsHydrophylic { get { return true; } }
        protected override bool HasChlorophyll { get { return false; } }
        public override bool IsPoisonous { get { return isPoisonous; } }
        protected override int Size
        {
            get { return size; }
            set { size = value.Bound(1, maxSize); }
        }

        static Fungus()
        {
            analyzed = new Dictionary<MushroomTypes, bool>();
            for (var i = MushroomTypes.White; i <= MushroomTypes.Galerina; i++)
            {
                analyzed[i] = false;
            }
        }

        public Fungus(string name, MushroomTypes type)
            : base(name)
        {
            mushroomType = type;
            this.DispatchMediator = new Mediator(this);
            switch (type)
            {
                case(MushroomTypes.White):
                    Size = 1;
                    maxSize = 3;
                    break;
                case(MushroomTypes.Truffle):
                    Size = 2;
                    maxSize = 9;
                    break;
                case (MushroomTypes.RedPine):
                    Size = 3;
                    maxSize = 7;
                    break;
                case(MushroomTypes.Amanita):
                    Size = 2;
                    maxSize = 13;
                    isPoisonous = true;
                    break;
                case(MushroomTypes.Galerina):
                    Size = 3;
                    maxSize = 6;
                    isPoisonous = true;
                    break;
            }
            Size = rand.Next(1, Size + 1);
            Type = mushroomType.ToString().ToLowerInvariant();
        }

        public override void Update(Simulation.WorldUpdateEventArgs args)
        {
            if (args.World.CurrentWeather == WeatherType.Rainy)
            {
                if (rand.Next(growChance) == 0) // The mushroom may or may not grow.
                {
                    (DispatchMediator as Mediator).WasWatered();
                }
            }
            if (CanGrow && (Size == maxSize) && (rand.Next(spawnChance) == 0))
            {
                var area = from hex in args.World.Map.EnumerateNeighbours(
                                PositionX, PositionY, 1)
                           from obj in hex.Objects
                           let tile = obj as GroundTile
                           where tile != null
                                && (int)tile.Humidity >= (int)IntensityLevel.AboveAverage
                           select tile;
                var tiles = area.ToArray();
                if (tiles.Length > 0)
                {
                    var hex = tiles[rand.Next(tiles.Length)];
                    spawned_count++;
                    var newFungus = new Fungus("{0}_{1}".Form(Name, spawned_count.ToString()),
                        mushroomType);
                    args.World.AddObject(newFungus, hex.PositionX, hex.PositionY);
                }
            }
        }

        public override void Marshal(DataCollection dataCollection)
        {
            base.Marshal(dataCollection);
            if (analyzed[mushroomType])
            {
                dataCollection.AddAttribute(Name, "poisonous", isPoisonous.ToString());
                dataCollection.AddAttribute(Name, "has_chlorophyll", HasChlorophyll.ToString());
                dataCollection.AddAttribute(Name, "hydrophylic", IsHydrophylic.ToString());
            }
        }
    }
}
