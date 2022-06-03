// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public abstract class ValidationException<T> : Exception
    {
        public T ErrorCode { get; }
        
        public ValidationException(T errorCode)
        {
            ErrorCode = errorCode;
        }

        public ValidationException(string message, T errorCode):base(message)
        {
            ErrorCode = errorCode;
        }

        public ValidationException(string message, T errorCode, Exception innerException):base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}