using System;
using MarginTrading.Backend.Contracts.Orders;

namespace MarginTrading.Backend.Contracts.Events
{
    public class OrderHistoryEvent
    {
        public OrderContract OrderSnapshot { get; set; }
        
        public OrderHistoryTypeContract Type { get; set; }
        
        public DateTime Timestamp { get; set; }
    }
}