using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class PositionNotFoundException : Exception
    {
        public PositionNotFoundException(string message)
            : base(message)
        {
        }
    }
}