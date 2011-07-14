using TerraSim.ForestWorld.Entities;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld
{
    class ForestEntityFactory
    {
        private ServiceMediator mediator;
        int seed = 100;

        public ForestEntityFactory(ServiceMediator mediator)
        {
            this.mediator = mediator;
        }

        public Fungus CreateFungus(Fungus.MushroomTypes type)
        {
            return new Fungus(GetName(type.ToString()), type);
        }

        public GreenPlant CreateGreenPlant(GreenPlant.GreenPlantTypes type)
        {
            return new GreenPlant(GetName(type.ToString()), type);
        }

        public AnimalFeeder CreateFeeder()
        {
            return new AnimalFeeder(GetName("feeder"));
        }

        public Animal CreateAnimal()
        {
            return new Animal(GetName("animal"), mediator);
        }

        private string GetName(string type)
        {
            return "{0}_{1}".Form(type, seed++);
        }
    }
}
