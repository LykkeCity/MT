// Copyright (c) 2019 Lykke Corp.

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
}