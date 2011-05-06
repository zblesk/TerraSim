using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Sensors
{
    public sealed class Postmortem : Sensor
    {
        public Postmortem(UserAgent owner) : base(owner)
        {
            LastInput.Clear();
            LastInput.AddAttribute(owner.Name, "dead", "true");
        }

        public override void Sense(WorldUpdateEventArgs args)
        {
            //The perception does not change. The owning agent is dead.
        }
    }
}
