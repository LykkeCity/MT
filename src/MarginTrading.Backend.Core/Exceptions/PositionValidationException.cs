// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class PositionValidationException : ValidationException<PositionValidationError>
    {
        public PositionValidationException(PositionValidationError errorCode) : base(errorCode)
        {
        }

        public PositionValidationException(string message, PositionValidationError errorCode) : base(message,
            errorCode)
        {
        }

        public PositionValidationException(string message,
            PositionValidationError errorCode,
            Exception innerException) : base(message, errorCode, innerException)
        {
        }
    }
}