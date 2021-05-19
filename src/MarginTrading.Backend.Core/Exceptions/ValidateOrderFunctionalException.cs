// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class ValidateOrderFunctionalException : ValidateOrderException
    {
        public ValidateOrderFunctionalException(OrderRejectReason reason, string rejectReasonText, string comment = null) :
            base(reason, rejectReasonText, comment)
        {
        }
    }
}