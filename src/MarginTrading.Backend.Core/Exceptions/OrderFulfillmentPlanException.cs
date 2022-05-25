// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class OrderFulfillmentPlanException : Exception
    {
        public OrderFulfillmentPlanException(string message) : base(message)
        {
        }
    }
}