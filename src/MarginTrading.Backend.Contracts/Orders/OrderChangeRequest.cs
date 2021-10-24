// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class OrderChangeRequest
    {
        public decimal Price { get; set; }

        public OriginatorTypeContract Originator { get; set; }
        
        public string AdditionalInfo { get; set; }
        
        public bool? ForceOpen { get; set; }

        public string AccountId { get; set; }
    }
}