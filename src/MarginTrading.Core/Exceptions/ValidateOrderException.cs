using System;

namespace MarginTrading.Core.Exceptions
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