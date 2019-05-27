using System;

namespace MarginTrading.Backend.Contracts.Orders
{
    public class RelatedOrderInfoContract
    {
        public OrderTypeContract Type { get; set; }
        public string Id { get; set; }
        
        public decimal Price { get; set; }
        
        public OrderStatusContract Status { get; set; }
        
        public DateTime ModifiedTimestamp { get; set; }

        /// <summary>
        /// Max distance between order price and parent order price (only for trailing order)
        /// </summary>
        public decimal? TrailingDistance { get; set; }
    }
}