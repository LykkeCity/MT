// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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