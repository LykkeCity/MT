using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.Trading
{
    //TODO: add validations
    public class Order
    {

        #region Properties
        
        /// <summary>
        /// Order ID
        /// </summary>
        public string Id { get; }
        
        /// <summary>
        /// Digit order code
        /// </summary>
        public long Code { get; }
        
        /// <summary>
        /// Asset Pair ID (eg. EURUSD) 
        /// </summary>
        public string AssetPairId { get; }
        
        /// <summary>
        /// Order size 
        /// </summary>
        public decimal Volume { get; private set; }
        
        /// <summary>
        /// Ordr direction (Buy/Sell)
        /// </summary>
        public OrderDirection Direction { get; }
        
        /// <summary>
        /// Date when order was created 
        /// </summary>
        public DateTime Created { get; }
        
        /// <summary>
        /// Date when order was activated 
        /// </summary>
        public DateTime Activated { get; private set; }
        
        /// <summary>
        /// Date when order was modified 
        /// </summary>
        public DateTime LastModified { get; private set; }
        
        /// <summary>
        /// Date when order will expire (null for Market)
        /// </summary>
        public DateTime? Validity { get; }
        
        /// <summary>
        /// Date when order started execution
        /// </summary>
        public DateTime? ExecutionStarted { get; private set; }
        
        /// <summary>
        /// Date when order was executed
        /// </summary>
        public DateTime? Executed { get; private set; }
        
        /// <summary>
        /// Date when order was canceled
        /// </summary>
        public DateTime? Canceled { get; private set; }
        
        /// <summary>
        /// Date when order was rejected
        /// </summary>
        public DateTime? Rejected { get; private set; }
        
        /// <summary>
        /// Trading account ID
        /// </summary>
        public string AccountId { get; }
        
        /// <summary>
        /// Trading conditions ID
        /// </summary>
        public string TradingConditionId { get; }
        
        /// <summary>
        /// Account base asset ID
        /// </summary>
        public string AccountAssetId { get; }

        /// <summary>
        /// Price level when order should be executed
        /// </summary>
        public decimal? Price { get; private set; }
        
        /// <summary>
        /// Price of order execution
        /// </summary>
        public decimal? ExecutionPrice { get; private set; }
        
        /// <summary>
        /// Asset for representation of equivalent price
        /// </summary>
        public string EquivalentAsset { get; }
        
        /// <summary>
        /// Rate for calculation of equivalent price
        /// </summary>
        public decimal EquivalentRate { get; private set; }
        
        /// <summary>
        /// Rate for calculation of price in account asset
        /// </summary>
        public decimal FxRate { get; private set; }
        
        /// <summary>
        /// Current order status
        /// </summary>
        public OrderStatus Status { get; private set; }
        
        /// <summary>
        /// Order fill type
        /// </summary>
        public OrderFillType FillType { get; }
        
        /// <summary>
        /// Reject reason
        /// </summary>
        public OrderRejectReason RejectReason { get; private set; }
        
        /// <summary>
        /// Human-readable reject reason
        /// </summary>
        public string RejectReasonText { get; private set; }
        
        /// <summary>
        /// Additional comment
        /// </summary>
        public string Comment { get; private set; }
        
        /// <summary>
        /// ID of exernal order (for STP mode)
        /// </summary>
        public string ExternalOrderId { get; private set; }
        
        /// <summary>
        /// ID of exernal LP (for STP mode)
        /// </summary>
        public string ExternalProviderId { get; private set; }
        
        /// <summary>
        /// Matching engine ID
        /// </summary>
        public string MatchingEngineId { get; private set; }
        
        /// <summary>
        /// Legal Entity ID
        /// </summary>
        public string LegalEntity { get; }
        
        /// <summary>
        /// Force open of new position
        /// </summary>
        public bool ForceOpen { get; }
        
        /// <summary>
        /// Order type
        /// </summary>
        public OrderType OrderType { get; }
        
        /// <summary>
        /// ID of parent order (for related orders)
        /// </summary>
        public string ParentOrderId { get; private set; } 
        
        /// <summary>
        /// ID of parent position (for related orders)
        /// </summary>
        public string ParentPositionId { get; private set; }

        /// <summary>
        /// Order originator
        /// </summary>
        public OriginatorType Originator { get; }
        
        /// <summary>
        /// Matched orders for execution
        /// </summary>
        public MatchedOrderCollection MatchedOrders { get; private set; }
        
        /// <summary>
        /// Related orders
        /// </summary>
        public List<RelatedOrderInfo> RelatedOrders { get; }
        
        #endregion


        public Order(string id, long code, string assetPairId, decimal volume,
            DateTime created, DateTime lastModified,
            DateTime? validity, string accountId, string tradingConditionId, string accountAssetId, decimal? price,
            string equivalentAsset, OrderFillType fillType, string comment, string legalEntity, bool forceOpen,
            OrderType orderType, string parentOrderId, string parentPositionId, OriginatorType originator,
            decimal equivalentRate, decimal fxRate)
        {
            Id = id;
            Code = code;
            AssetPairId = assetPairId;
            Volume = volume;
            Created = created;
            LastModified = lastModified;
            Validity = validity;
            AccountId = accountId;
            TradingConditionId = tradingConditionId;
            AccountAssetId = accountAssetId;
            Price = price;
            EquivalentAsset = equivalentAsset;
            FillType = fillType;
            Comment = comment;
            LegalEntity = legalEntity;
            ForceOpen = forceOpen;
            OrderType = orderType;
            ParentOrderId = parentOrderId;
            ParentPositionId = parentPositionId;
            Originator = originator;
            EquivalentRate = equivalentRate;
            FxRate = fxRate;
            Direction = volume.GetOrderDirection();
            Status = OrderStatus.Placed;
            MatchedOrders = new MatchedOrderCollection();
            RelatedOrders = new List<RelatedOrderInfo>();
        }


        #region Actions

        public void ChangePrice(decimal newPrice, DateTime dateTime)
        {
            LastModified = dateTime;
            Price = newPrice;
        }
        
        public void ChangeVolume(decimal newVolume, DateTime dateTime)
        {
            LastModified = dateTime;
            Volume = newVolume;
        }

        public void MakeInactive(DateTime dateTime)
        {
            Status = OrderStatus.Inactive;
            LastModified = dateTime;
        }
        
        public void Activate(DateTime dateTime, bool relinkFromOrderToPosition)
        {
            Status = OrderStatus.Active;
            Activated = dateTime;
            LastModified = dateTime;

            if (relinkFromOrderToPosition)
            {
                ParentPositionId = ParentOrderId;
            }
        }
        
        public void StartExecution(DateTime dateTime, string matchingEngineId)
        {
            Status = OrderStatus.ExecutionStarted;
            ExecutionStarted = dateTime;
            LastModified = dateTime;
            MatchingEngineId = matchingEngineId;
        }
        
        public void Execute(DateTime dateTime, MatchedOrderCollection matchedOrders)
        {
            Status = OrderStatus.Executed;
            
            var externalOrderId = string.Empty;
            var externalProviderId = string.Empty;
                
            if (matchedOrders.Count == 1)
            {
                var matched = matchedOrders.First();

                if (matched.IsExternal)
                {
                    externalOrderId = matched.OrderId;
                    externalProviderId = matched.MarketMakerId;
                }
            }
            
            Executed = dateTime;
            LastModified = dateTime;
            ExecutionPrice = matchedOrders.WeightedAveragePrice;
            ExternalOrderId = externalOrderId;
            ExternalProviderId = externalProviderId;
            MatchedOrders = matchedOrders;
        }

        public void SetRates(decimal equivalentRate, decimal fxRate)
        {
            EquivalentRate = equivalentRate;
            FxRate = fxRate;
        }

        public void Reject(OrderRejectReason reason, string reasonText, string comment, DateTime dateTime)
        {
            Status = OrderStatus.Rejected;
            RejectReason = reason;
            RejectReasonText = reasonText;
            Comment = comment;
            Rejected = dateTime;
            LastModified = dateTime;
        }

        public void Cancel(DateTime dateTime)
        {
            Status = OrderStatus.Canceled;
            Canceled = dateTime;
            LastModified = dateTime;
        }

        public void AddRelatedOrder(Order order)
        {
            var info = new RelatedOrderInfo {Type = order.OrderType, Id = order.Id};
            
            if (!RelatedOrders.Contains(info))
                RelatedOrders.Add(info);
        }

        #endregion
        
    }
}