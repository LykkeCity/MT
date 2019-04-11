using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core.Orders
{
    public interface IOrderHistory
    {
        /// <summary>
        /// Order id 
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Account id
        /// </summary>
        string AccountId { get; }

        /// <summary>
        /// Instrument id (e.g."BTCUSD", where BTC - base asset unit, USD - quoting unit)
        /// </summary>
        string AssetPairId { get; }
        
        /// <summary>
        /// Parent order id. Filled if it's a related order.
        /// </summary>
        [CanBeNull]
        string ParentOrderId { get; }
        
        /// <summary>
        /// Parent position id. Filled if it's a related order.
        /// </summary>
        [CanBeNull]
        string PositionId { get; }

        /// <summary>
        /// The order direction (Buy or Sell)
        /// </summary>
        OrderDirection Direction { get; }

        /// <summary>
        /// The order type (Market, Limit, Stop, TakeProfit, StopLoss or TrailingStop)
        /// </summary>
        OrderType Type { get; }

        /// <summary>
        /// The order status (Active, Inactive, Executed, Canceled, Rejected or Expired)
        /// </summary>
        OrderStatus Status { get; }
        
        /// <summary>
        /// Order fill type
        /// </summary>
        OrderFillType FillType { get; }

        /// <summary>
        /// Who created the order (Investor, System or OnBehalf)
        /// </summary>
        OriginatorType Originator { get; }

        /// <summary>
        /// Who cancelled/rejected the order (Investor, System or OnBehalf)
        /// </summary>
        OriginatorType? CancellationOriginator { get; }
        
        /// <summary>
        /// Order volume in base asset units. Not filled for related orders (TakeProfit, StopLoss or TrailingStop).
        /// </summary>
        decimal Volume { get; }

        /// <summary>
        /// Expected open price (in quoting asset units per one base unit). Not filled for Market orders.
        /// </summary>
        decimal? ExpectedOpenPrice { get; }

        /// <summary>
        /// Execution open price (in quoting asset units per one base unit). Filled for executed orders only.
        /// </summary>
        decimal? ExecutionPrice { get; }
        
        /// <summary>
        /// Current FxRate
        /// </summary>
        decimal FxRate { get; }
        
        /// <summary>
        /// FX asset pair id
        /// </summary>
        string FxAssetPairId { get; }
        
        /// <summary>
        /// Shows if account asset id is directly related on asset pair quote asset.
        /// I.e. AssetPair is {BaseId, QuoteId} and FxAssetPair is {QuoteId, AccountAssetId} => Straight
        /// If AssetPair is {BaseId, QuoteId} and FxAssetPair is {AccountAssetId, QuoteId} => Reverse
        /// </summary>
        FxToAssetPairDirection FxToAssetPairDirection { get; }

        /// <summary>
        /// Force open separate position for the order, ignoring existing ones
        /// </summary>
        bool ForceOpen { get; }

        /// <summary>
        /// Till validity time
        /// </summary>
        DateTime? ValidityTime { get; }

        /// <summary>
        /// Creation date and time
        /// </summary>
        DateTime CreatedTimestamp { get; }
        
        /// <summary>
        /// Last modification date and time
        /// </summary>
        DateTime ModifiedTimestamp { get; }
        
////--------------
        
        /// <summary>
        /// Digit order code
        /// </summary>
        long Code { get; }
        
        /// <summary>
        /// Date when order was activated 
        /// </summary>
        DateTime? ActivatedTimestamp { get; }
        
        /// <summary>
        /// Date when order started execution
        /// </summary>
        DateTime? ExecutionStartedTimestamp { get; }
        
        /// <summary>
        /// Date when order was executed
        /// </summary>
        DateTime? ExecutedTimestamp { get; }
        
        /// <summary>
        /// Date when order was canceled
        /// </summary>
        DateTime? CanceledTimestamp { get; }
        
        /// <summary>
        /// Date when order was rejected
        /// </summary>
        DateTime? Rejected { get; }
        
        /// <summary>
        /// Trading conditions ID
        /// </summary>
        string TradingConditionId { get; }
        
        /// <summary>
        /// Account base asset ID
        /// </summary>
        string AccountAssetId { get; }

        /// Asset for representation of equivalent price
        /// </summary>
        string EquivalentAsset { get; }
        
        /// <summary>
        /// Rate for calculation of equivalent price
        /// </summary>
        decimal EquivalentRate { get; }
        
        /// <summary>
        /// Reject reason
        /// </summary>
        OrderRejectReason RejectReason { get; }
        
        /// <summary>
        /// Human-readable reject reason
        /// </summary>
        string RejectReasonText { get; }
        
        /// <summary>
        /// Additional comment
        /// </summary>
        string Comment { get; }
        
        /// <summary>
        /// ID of exernal order (for STP mode)
        /// </summary>
        string ExternalOrderId { get; }
        
        /// <summary>
        /// ID of external LP (for STP mode)
        /// </summary>
        string ExternalProviderId { get; }
        
        /// <summary>
        /// Matching engine ID
        /// </summary>
        string MatchingEngineId { get; }
        
        /// <summary>
        /// Legal Entity ID
        /// </summary>
        string LegalEntity { get; }
        
        /// <summary>
        /// Matched orders for execution
        /// </summary>
        List<MatchedOrder> MatchedOrders { get; }
        
        /// <summary>
        /// Related orders
        /// </summary>
        List<RelatedOrderInfo> RelatedOrderInfos { get; }
        
        OrderUpdateType UpdateType { get; }
        
        string AdditionalInfo { get; }
        
        /// <summary>
        /// The correlation identifier.
        /// In every operation that results in the creation of a new message the correlationId should be copied from
        /// the inbound message to the outbound message. This facilitates tracking of an operation through the system.
        /// If there is no inbound identifier then one should be created eg. on the service layer boundary (API).  
        /// </summary>
        string CorrelationId { get; }
        
        /// <summary>
        /// Number of pending order retries passed
        /// </summary>
        int PendingOrderRetriesCount { get; }
    }
}