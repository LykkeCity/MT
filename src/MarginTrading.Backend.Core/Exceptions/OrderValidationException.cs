// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class OrderValidationException : ValidationException<OrderValidationError>
    {
        public OrderValidationException(OrderValidationError errorCode) : base(errorCode)
        {
        }

        public OrderValidationException(string message, OrderValidationError errorCode) : base(message, errorCode)
        {
        }

        public OrderValidationException(string message, OrderValidationError errorCode, Exception innerException) :
            base(message, errorCode, innerException)
        {
        }
    }
}