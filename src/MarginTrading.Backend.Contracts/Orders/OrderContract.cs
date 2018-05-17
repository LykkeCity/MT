using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Contracts.Orders
{
    /// <summary>
    /// Info about an order
    /// </summary>
    public class OrderContract
    {
        /// <summary>
        /// Order id 
        /// </summary>
        public string Id { get; set; }
        
        /// <summary>
        /// Account id
        /// </summary>
        public string AccountId { get; set; }
        
        /// <summary>
        /// Instrument id
        /// </summary>
        public string AssetPairId { get; set; }
        
        /// <summary>
        /// Parent order id. Filled if it's a related order.
        /// </summary>
        [CanBeNull]
        public string ParentOrderId { get; set; }
        
        /// <summary>
        /// Position id. Filled if it's a basic executed order.
        /// </summary>
        [CanBeNull]
        public string PositionId { get; set; }

        /// <summary>
        /// The order direction (buy or sell)
        /// </summary>
        public OrderDirectionContract Direction { get; set; }
        
        /// <summary>
        /// The order type (Market, Limit, etc.)
        /// </summary>
        public OrderTypeContract Type { get; set; }
        
        /// <summary>
        /// The order status (Active, Executed, etc.)
        /// </summary>
        public OrderStatusContract Status { get; set; }
        
        /// <summary>
        /// Who created the order
        /// </summary>
        public OriginatorTypeContract Originator { get; set; }

        /// <summary>
        /// Order volume in quoting asset units. Not filled for related orders.
        /// </summary>
        public decimal? Volume { get; set; }
        
        /// <summary>
        /// Expected open price in base asset units. Not filled for market orders.
        /// </summary>
        public decimal? ExpectedOpenPrice { get; set; }
        
        /// <summary>
        /// Execution open price in base asset units. Filled for executed orders only.
        /// </summary>
        public decimal? ExecutionPrice { get; set; }

        /// <summary>
        /// Execution trades ids. Filled for executed orders only.
        /// If the order execution affected multiple positions - there will be multiple trades.
        /// </summary>
        [CanBeNull]
        public List<string> TradesIds { get; set; }

        /// <summary>
        /// The related orders
        /// </summary>
        public List<string> RelatedOrders { get; set; }

        /// <summary>
        /// Force open a sepatate position, ignoring any exising ones
        /// </summary>
        public bool ForceOpen { get; set; }

        /// <summary>
        /// Till validity time
        /// </summary>
        public DateTime? ValidityTime { get; set; }
        
        /// <summary>
        /// Creation date and time
        /// </summary>
        public DateTime CreatedTimestamp { get; set; }
        
        /// <summary>
        /// Last modification date and time
        /// </summary>
        public DateTime ModifiedTimestamp { get; set; }
    }
}