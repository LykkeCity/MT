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
        /// Instrument id (e.g."BTCUSD", where BTC - base asset unit, USD - quoting unit)
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
        /// The order direction (Buy or Sell)
        /// </summary>
        public OrderDirectionContract Direction { get; set; }

        /// <summary>
        /// The order type (Market, Limit, Stop, TakeProfit, StopLoss or TrailingStop)
        /// </summary>
        public OrderTypeContract Type { get; set; }

        /// <summary>
        /// The order status (Active, Inactive, Executed, Canceled, Rejected or Expired)
        /// </summary>
        public OrderStatusContract Status { get; set; }

        /// <summary>
        /// Who created the order (Investor, System or OnBehalf)
        /// </summary>
        public OriginatorTypeContract Originator { get; set; }

        /// <summary>
        /// Order volume in base asset units. Not filled for related orders (TakeProfit, StopLoss or TrailingStop).
        /// </summary>
        public decimal? Volume { get; set; }

        /// <summary>
        /// Expected open price (in quoting asset units per one base unit). Not filled for Market orders.
        /// </summary>
        public decimal? ExpectedOpenPrice { get; set; }

        /// <summary>
        /// Execution open price (in quoting asset units per one base unit). Filled for executed orders only.
        /// </summary>
        public decimal? ExecutionPrice { get; set; }
        
        /// <summary>
        /// Current FxRate
        /// </summary>
        public decimal FxRate { get; set; }

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
        /// Force open separate position for the order, ignoring existing ones
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