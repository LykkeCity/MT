// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class PositionGroupValidationException : ValidationException<PositionGroupValidationError>
    {
        public PositionGroupValidationException(PositionGroupValidationError errorCode) : base(errorCode)
        {
        }

        public PositionGroupValidationException(string message, PositionGroupValidationError errorCode) : base(message,
            errorCode)
        {
        }

        public PositionGroupValidationException(string message,
            PositionGroupValidationError errorCode,
            Exception innerException) : base(message, errorCode, innerException)
        {
        }
    }
}