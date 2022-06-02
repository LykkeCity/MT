// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class ValidateOrderException : Exception
    {
        public OrderRejectReason RejectReason { get; private set; }
        public string Comment { get; private set; }

        public ValidateOrderException(OrderRejectReason reason, string rejectReasonText, string comment = null):base(rejectReasonText)
        {
            RejectReason = reason;
            Comment = comment;
        }
    }

    public enum AccountValidationError
    {
        None = 0,
        AccountDisabled,
        AccountDoesNotExist
    }

    public enum InstrumentValidationError
    {
        None = 0,
        InstrumentTradingDisabled,
        TradesAreNoAvailable
    }

    public class ValidationException<T> : Exception where T : Enum
    {
        public T ErrorCode { get; private set; }
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