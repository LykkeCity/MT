using System;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class OrderChangeRequest
    {
        public decimal Volume { get; set; }
        
        public decimal Price { get; set; }
        
        public bool ForceOpen { get; set; }

        public DateTime? Validity { get; set; }
    }
}