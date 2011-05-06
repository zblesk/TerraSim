using System;
using System.Linq;
using TerraSim.Network;
using TerraSim.Simulation;

namespace TerraSim.ForestWorld.Actuators
{
    public sealed class Wheels : Actuator
    {
        private static readonly string[] actions = 
            new string[] { "motor_stats", "left", "right", "forward", "backward" };
        /// <summary>a
        /// The action currently being executed. It is an index to the 'actions' array.
        /// </summary>
        private int currentAction = -1;
        private int timeRemainig = 0;
        private static int[][] evenLineOffsets =
            new int[][] { new int[] { -1, -1},new int[] { -1, 0},new int[] { 0, 1},
                new int[] { 1, 0}, new int[] { 1, -1},new int[] { 0, -1}};
        private static int[][] oddLineOffsets =
            new int[][] { new int[] { -1, 0 }, new int[] { -1,1 }, new int[] { 0,1 }, 
                new int[] { 1,1 }, new int[] { 1,0 }, new int[] { 0,-1 }};
        /// <summary>
        /// On a hex grid, the position of the next hex in certain direction is dependant
        /// on whether we're currently in an odd or in an even row (X coordinate.) 
        /// To account for this, we store offsets leading to neighbouring hexes separately 
        /// for odd and for even lines.
        /// </summary>
        private static int[][][] offsets = new int [][][] { evenLineOffsets, oddLineOffsets };
        /// <summary>
        /// Determines how much enery will each action cost.
        /// </summary>
        private int actionPrice = 5;
        #region Stats
        private bool sendStats = false;
        private int turnCount = 0;
        private int stepCount = 0;
        #endregion

        private int _curHeading; //a property backing field 
        /// <summary>
        /// The direction in which the agent is currently headed.
        /// </summary>
        private int CurrentHeading
        {
            get { return _curHeading; }
            set { _curHeading = value.Bound(0, 5); }
        }

        public Wheels(UserAgent owner, ServiceMediator sm) : base(owner, sm) { }
        
        public override string[] ActionList()
        {
            return actions;
        }

        public override void Marshal(DataCollection dataCollection)
        {
            dataCollection.AddAttribute(Owner.Name, "heading", CurrentHeading.ToString());
            if (sendStats)
            {
                dataCollection.AddAttribute(Owner.Name, "steps_taken", stepCount.ToString());
                dataCollection.AddAttribute(Owner.Name, "turns_made", turnCount.ToString());
                sendStats = false;
            }
            if ((currentAction >= 0) && (timeRemainig >= 0))
            {
                dataCollection.AddAction(Owner.Name, actions[currentAction], "", "");
            }
        }

        public override void Perform(WorldUpdateEventArgs args, string action,
            string argument1, string argument2)
        {
            action = action.ToLowerInvariant();
            if (action == "motor_stats")
            {
                sendStats = true;
            }
            else
            {
                Mediator.MessageBroadcaster.Broadcast(
                    TerraSim.ForestWorld.Sensors.Accumulator.EnergyPaymentCommand,
                    actionPrice, Owner,
                    (b) => { if ((bool)b) ChooseAction(action); }, Owner.Name);
            }
        }

        public override void Update(WorldUpdateEventArgs args)
        {
            if (currentAction >= 0)
            {
                if (timeRemainig == 0)
                {
                    PerformAction(args);
                    currentAction = -1;
                }
                else
                {
                    timeRemainig--;
                }
            }
        }
        
        private void ChooseAction(string action)
        {
            switch (action)
            {
                case ("left"):
                    currentAction = 1;
                    timeRemainig = 1;
                    break;
                case ("right"):
                    currentAction = 2;
                    timeRemainig = 1;
                    break;
                case ("forward"):
                    currentAction = 3;
                    timeRemainig = 1;
                    break;
                case ("backward"):
                    currentAction = 4;
                    timeRemainig = 1;
                    break;
                default:
                    throw new Exception("Unknown action name.");
            }
        }

        private void PerformAction(WorldUpdateEventArgs args)
        {
            switch (currentAction)
            {
                case(1):
                    CurrentHeading--;
                    turnCount++;
                    break;
                case(2):
                    CurrentHeading++;
                    turnCount++;
                    break;
                case(3):
                    StepForward(args);
                    stepCount++;
                    break;
                case (4):
                    CurrentHeading = (CurrentHeading + 3) % 6;
                    StepForward(args);
                    CurrentHeading = (CurrentHeading + 3) % 6;
                    stepCount++;
                    break;
                default:
                    throw new Exception("Internal inconsistency encountered - unknown action.");
            }
        }

        private void StepForward(WorldUpdateEventArgs args)
        {
            int offset = Owner.PositionX % 2;
            int newX = offsets[offset][CurrentHeading][0] + Owner.PositionX;
            int newY = offsets[offset][CurrentHeading][1] + Owner.PositionY;
            if (args.World.Map.Contains(newX, newY))
            {
                var t = args.World.Map[newX, newY].Objects.First((n) => n is GroundTile);
                var tile = t as GroundTile;
                if (tile.SpeedCap > 0) //if it is 0, agent can't step on the tile (it is inaccessible.)
                {
                    args.World.MoveObject(Owner, newX, newY);
                }
            }
        }
    }
}
