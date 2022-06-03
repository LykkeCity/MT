// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public sealed class InstrumentValidationException : ValidationException<InstrumentValidationError>
    {
        public InstrumentValidationException(InstrumentValidationError errorCode) : base(errorCode)
        {
        }

        public InstrumentValidationException(string message, InstrumentValidationError errorCode) : base(message, errorCode)
        {
        }

        public InstrumentValidationException(string message, InstrumentValidationError errorCode, Exception innerException) : base(message, errorCode, innerException)
        {
        }
    }
}