// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Contracts.Orders;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class ClosePositionGroupException : ValidationException<PositionGroupCloseError>
    {
        public ClosePositionGroupException(PositionGroupCloseError errorCode) : base(errorCode)
        {
        }

        public ClosePositionGroupException(string message, PositionGroupCloseError errorCode) : base(message, errorCode)
        {
        }

        public ClosePositionGroupException(string message, PositionGroupCloseError errorCode, Exception innerException)
            : base(message, errorCode, innerException)
        {
        }
    }
}