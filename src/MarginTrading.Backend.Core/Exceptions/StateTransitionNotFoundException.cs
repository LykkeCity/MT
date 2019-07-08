// Copyright (c) 2019 Lykke Corp.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class StateTransitionNotFoundException : Exception
    {
        public StateTransitionNotFoundException(string message)
            : base(message)
        {
        }
    }
}