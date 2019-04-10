using System;

namespace MarginTrading.Backend.Core.Orders
{
    public class RelatedOrderInfo : IEquatable<RelatedOrderInfo>
    {
        public OrderType Type { get; set; }
        public string Id { get; set; }
        
        public bool Equals(RelatedOrderInfo other)
        {
            return Id == other?.Id;
        }
    }
}