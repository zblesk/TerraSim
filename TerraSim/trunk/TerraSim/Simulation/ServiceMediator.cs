using System;
using System.Collections.Generic;

namespace TerraSim.Simulation
{
    public sealed class ServiceMediator
    {
        private World world;
        private List<Action> afterUpdateActions = new List<Action>();
        
        public MessageBroadcaster MessageBroadcaster { get; private set; }

        public ServiceMediator(World world)
        {
            this.world = world;
            MessageBroadcaster = world.MessageBroadcaster;
            world.AfterUpdate += AfterUpdate;
        }

        public void ExecuteAfterUpdate(Action action)
        {
            afterUpdateActions.Add(action);
        }

        private void AfterUpdate()
        {
            foreach (Action act in afterUpdateActions)
            {
                act();
            }
            afterUpdateActions.Clear();
        }

    }
}
