using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class UpdateRelatedOrderRequest
    {
        public string AccountId { get; set; }
        public OriginatorTypeContract Originator { get; set; }
        public RelatedOrderTypeContract OrderType { get; set; }
        public decimal NewPrice { get; set; }
        public string AdditionalInfoJson { get; set; }
        public bool? HasTrailingStop { get; set; }
    }
}
