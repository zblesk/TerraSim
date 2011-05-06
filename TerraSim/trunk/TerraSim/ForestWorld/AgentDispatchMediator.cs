using TerraSim.ForestWorld.Sensors;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld
{
    public sealed class AgentDispatchMediator
    {
        private readonly UserAgent owner;

        public ServiceMediator ServiceMediator { get; private set; }

        public AgentDispatchMediator(UserAgent owner, ServiceMediator mediator)
        {
            this.owner = owner;
            ServiceMediator = mediator;
        }

        public void WasShot()
        {
            ServiceMediator.ExecuteAfterUpdate(() =>
                {
                    owner.RemoveActuators();
                    owner.RemoveSensors();
                    owner.AddSensor(new Postmortem(owner));
                });
        }
    }
}
