using System;
using TerraSim.Simulation;

namespace TerraSim
{
    public delegate void PositionChanged(NamedObject sender, int oldX, int oldY, int newX, int newY);

    public class InvalidDataException : Exception
    {
        public InvalidDataException(string message) : base(message) { }
    }
}