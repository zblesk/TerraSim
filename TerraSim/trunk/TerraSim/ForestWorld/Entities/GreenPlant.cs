using System;
using System.Collections.Generic;
using System.Linq;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Entities
{
    public class GreenPlant : Plant
    {
        public enum GreenPlantTypes { Fern, Snowdrop, Narcissus, Datura }

        internal class Mediator : PlantMediator
        {
            private GreenPlant Owner { get { return owner as GreenPlant; } }

            public Mediator(GreenPlant owner)
                : base(owner)
            {
            }

            internal void WasWatered()
            {
                if (Owner.IsHydrophylic)
                {
                    Owner.Size += 2;
                }
                else
                {
                    Owner.Size++;
                }
            }

            internal void WasAnalyzed()
            {
                analyzed[Owner.plantType] = true;
            }
        }

        private static Random rand = new Random();
        private static Dictionary<GreenPlantTypes, bool> analyzed;
        private const int growChance = 6;
        private const int spawnChance = 40;
        private const int sizeRequiredToSpread = 7;
        /// <summary>
        /// Indicates how far away a new plant can grow.
        /// </summary>
        private const int maximalSpreadDistance = 2; 
        private bool isPoisonous = false, isHydrophylic = false;
        private int size = 1, maxSize = 1;
        private GreenPlantTypes plantType;
        private int spawned_count = 0;

        protected override bool HasChlorophyll { get { return true; } }
        protected override bool IsHydrophylic { get { return isHydrophylic; } }
        public override bool IsPoisonous { get { return isPoisonous; } }
        protected override int Size
        {
            get { return size; }
            set { size = value.Bound(1, maxSize); }
        }

        static GreenPlant()
        {
            analyzed = new Dictionary<GreenPlantTypes, bool>();
            for (var i = GreenPlantTypes.Fern; i <= GreenPlantTypes.Narcissus; i++)
            {
                analyzed[i] = false;
            }
        }

        public GreenPlant(string name, GreenPlantTypes type)
            : base(name)
        {
            plantType = type;
            this.DispatchMediator = new Mediator(this);
            switch (type)
            {
                case GreenPlantTypes.Fern:
                    maxSize = 37;
                    isHydrophylic = true;
                    break;
                case GreenPlantTypes.Snowdrop:
                    maxSize = 9;
                    break;
                case GreenPlantTypes.Narcissus:
                    maxSize = 21;
                    isPoisonous = true;
                    break;
                case GreenPlantTypes.Datura:
                    maxSize = 50;
                    isHydrophylic = isPoisonous = true;
                    break;
                default:
                    throw new Exception("Impossible plant type.");
            }
            Size = 1;
            Type = plantType.ToString().ToLowerInvariant();
        }

        public override void Update(Simulation.WorldUpdateEventArgs args)
        {
            var gtile = args.World.Map[PositionX, PositionY].Objects.First(
                (o) => {return o is GroundTile;}) as GroundTile;
            var level = (int)gtile.Lighting;
            if (level > (int)IntensityLevel.Average) // grow if in sunshine
            {
                var max = (int)IntensityLevel.Full - level;
                if (rand.Next(2, (int)IntensityLevel.Full) < max)
                {
                    Size += 1;
                }
            }
            if (args.World.CurrentWeather == WeatherType.Rainy)
            {
                if (rand.Next(growChance) == 0) 
                {
                    (DispatchMediator as Mediator).WasWatered();
                }
            }
            if (CanGrow && (Size >= sizeRequiredToSpread) && (rand.Next(spawnChance) == 0))
            {
                var area = from hex in args.World.Map.EnumerateNeighbours(
                                PositionX, PositionY, maximalSpreadDistance)
                           from obj in hex.Objects
                           let tile = obj as GroundTile
                           where tile != null
                           select tile;
                var tiles = area.ToArray();
                if (tiles.Length > 0)
                {
                    var hex = tiles[rand.Next(tiles.Length)];
                    spawned_count++;
                    var newPlant = new GreenPlant("{0}_{1}".Form(Name, spawned_count.ToString()),
                        plantType);
                    args.World.AddObject(newPlant, hex.PositionX, hex.PositionY);
                }
            }
        }

        public override void Marshal(DataCollection dataCollection)
        {
            base.Marshal(dataCollection);
            if (analyzed[plantType])
            {
                dataCollection.AddAttribute(Name, "poisonous", isPoisonous.ToString());
                dataCollection.AddAttribute(Name, "has_chlorophyll", HasChlorophyll.ToString());
                dataCollection.AddAttribute(Name, "hydrophylic", IsHydrophylic.ToString());
            }
        }
    }
}
