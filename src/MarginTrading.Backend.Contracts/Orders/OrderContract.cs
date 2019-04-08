using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.TradeMonitoring;

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
        /// Parent position id. Filled if it's a related order.
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
        /// Who changed the order (Investor, System or OnBehalf)
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
        /// FX asset pair id
        /// </summary>
        public string FxAssetPairId { get; set; }
        
        /// <summary>
        /// Shows if account asset id is directly related on asset pair quote asset.
        /// I.e. AssetPair is {BaseId, QuoteId} and FxAssetPair is {QuoteId, AccountAssetId} => Straight
        /// If AssetPair is {BaseId, QuoteId} and FxAssetPair is {AccountAssetId, QuoteId} => Reverse
        /// </summary>
        public FxToAssetPairDirectionContract FxToAssetPairDirection { get; set; }

        /// <summary>
        /// The related orders
        /// </summary>
        [Obsolete]
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
        
////--------------
        
        /// <summary>
        /// Digit order code
        /// </summary>
        public long Code { get; set; }
        
        /// <summary>
        /// Date when order was activated 
        /// </summary>
        public DateTime? ActivatedTimestamp { get; set; }
        
        /// <summary>
        /// Date when order started execution
        /// </summary>
        public DateTime? ExecutionStartedTimestamp { get; set; }
        
        /// <summary>
        /// Date when order was executed
        /// </summary>
        public DateTime? ExecutedTimestamp { get; set; }
        
        /// <summary>
        /// Date when order was canceled
        /// </summary>
        public DateTime? CanceledTimestamp { get; set; }
        
        /// <summary>
        /// Date when order was rejected
        /// </summary>
        public DateTime? Rejected { get; set; }
        
        /// <summary>
        /// Trading conditions ID
        /// </summary>
        public string TradingConditionId { get; set; }
        
        /// <summary>
        /// Account base asset ID
        /// </summary>
        public string AccountAssetId { get; set; }

        /// <summary>
        /// Asset for representation of equivalent price
        /// </summary>
        public string EquivalentAsset { get; set; }
        
        /// <summary>
        /// Rate for calculation of equivalent price
        /// </summary>
        public decimal EquivalentRate { get; set; }
        
        /// <summary>
        /// Reject reason
        /// </summary>
        public OrderRejectReasonContract RejectReason { get; set; }
        
        /// <summary>
        /// Human-readable reject reason
        /// </summary>
        public string RejectReasonText { get; set; }
        
        /// <summary>
        /// Additional comment
        /// </summary>
        public string Comment { get; set; }
        
        /// <summary>
        /// ID of exernal order (for STP mode)
        /// </summary>
        public string ExternalOrderId { get; set; }
        
        /// <summary>
        /// ID of exernal LP (for STP mode)
        /// </summary>
        public string ExternalProviderId { get; set; }
        
        /// <summary>
        /// Matching engine ID
        /// </summary>
        public string MatchingEngineId { get; set; }
        
        /// <summary>
        /// Legal Entity ID
        /// </summary>
        public string LegalEntity { get; set; }
        
        /// <summary>
        /// Matched orders for execution
        /// </summary>
        public List<MatchedOrderContract> MatchedOrders { get; set; }
        
        /// <summary>
        /// Related orders
        /// </summary>
        public List<RelatedOrderInfoContract> RelatedOrderInfos { get; set; }

        /// <summary>
        /// Additional info from last user request
        /// </summary>
        public string AdditionalInfo { get; set; }
        
        /// <summary>
        /// The correlation identifier.
        /// In every operation that results in the creation of a new message the correlationId should be copied from
        /// the inbound message to the outbound message. This facilitates tracking of an operation through the system.
        /// If there is no inbound identifier then one should be created eg. on the service layer boundary (API).  
        /// </summary>
        public string CorrelationId { get; set; }
        
        /// <summary>
        /// Number of pending order retries passed
        /// </summary>
        public int PendingOrderRetriesCount { get; set; }
    }
}