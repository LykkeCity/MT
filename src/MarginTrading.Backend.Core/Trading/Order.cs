using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orders;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.Trading
{
    //TODO: add validations
    public class Order
    {

        #region Properties
        
        
        /// <summary>
        /// Order ID
        /// </summary>
        [JsonProperty]
        public string Id { get; private set; }
        
        /// <summary>
        /// Digit order code
        /// </summary>
        [JsonProperty]
        public long Code { get; private set; }
        
        /// <summary>
        /// Asset Pair ID (eg. EURUSD) 
        /// </summary>
        [JsonProperty]
        public string AssetPairId { get; private set; }
        
        /// <summary>
        /// Order size 
        /// </summary>
        [JsonProperty]
        public decimal Volume { get; private set; }
        
        /// <summary>
        /// Ordr direction (Buy/Sell)
        /// </summary>
        [JsonProperty]
        public OrderDirection Direction { get; private set; }
        
        /// <summary>
        /// Date when order was created 
        /// </summary>
        [JsonProperty]
        public DateTime Created { get; private set; }
        
        /// <summary>
        /// Date when order was activated 
        /// </summary>
        [JsonProperty]
        public DateTime? Activated { get; private set; }
        
        /// <summary>
        /// Date when order was modified 
        /// </summary>
        [JsonProperty]
        public DateTime LastModified { get; private set; }
        
        /// <summary>
        /// Date when order will expire (null for Market)
        /// </summary>
        [JsonProperty]
        public DateTime? Validity { get; private set; }
        
        /// <summary>
        /// Date when order started execution
        /// </summary>
        [JsonProperty]
        public DateTime? ExecutionStarted { get; private set; }
        
        /// <summary>
        /// Date when order was executed
        /// </summary>
        [JsonProperty]
        public DateTime? Executed { get; private set; }
        
        /// <summary>
        /// Date when order was canceled
        /// </summary>
        [JsonProperty]
        public DateTime? Canceled { get; private set; }
        
        /// <summary>
        /// Date when order was rejected
        /// </summary>
        [JsonProperty]
        public DateTime? Rejected { get; private set; }
        
        /// <summary>
        /// Trading account ID
        /// </summary>
        [JsonProperty]
        public string AccountId { get; private set; }
        
        /// <summary>
        /// Trading conditions ID
        /// </summary>
        [JsonProperty]
        public string TradingConditionId { get; private set; }
        
        /// <summary>
        /// Account base asset ID
        /// </summary>
        [JsonProperty]
        public string AccountAssetId { get; private set; }

        /// <summary>
        /// Price level when order should be executed
        /// </summary>
        [JsonProperty]
        public decimal? Price { get; private set; }
        
        /// <summary>
        /// Price of order execution
        /// </summary>
        [JsonProperty]
        public decimal? ExecutionPrice { get; private set; }
        
        /// <summary>
        /// Asset for representation of equivalent price
        /// </summary>
        [JsonProperty]
        public string EquivalentAsset { get; private set; }
        
        /// <summary>
        /// Rate for calculation of equivalent price
        /// </summary>
        [JsonProperty]
        public decimal EquivalentRate { get; private set; }
        
        /// <summary>
        /// Rate for calculation of price in account asset
        /// </summary>
        [JsonProperty]
        public decimal FxRate { get; private set; }
        
        /// <summary>
        /// Current order status
        /// </summary>
        [JsonProperty]
        public OrderStatus Status { get; private set; }
        
        /// <summary>
        /// Order fill type
        /// </summary>
        [JsonProperty]
        public OrderFillType FillType { get; private set; }
        
        /// <summary>
        /// Reject reason
        /// </summary>
        [JsonProperty]
        public OrderRejectReason RejectReason { get; private set; }
        
        /// <summary>
        /// Human-readable reject reason
        /// </summary>
        [JsonProperty]
        public string RejectReasonText { get; private set; }
        
        /// <summary>
        /// Additional comment
        /// </summary>
        [JsonProperty]
        public string Comment { get; private set; }
        
        /// <summary>
        /// ID of exernal order (for STP mode)
        /// </summary>
        [JsonProperty]
        public string ExternalOrderId { get; private set; }
        
        /// <summary>
        /// ID of exernal LP (for STP mode)
        /// </summary>
        [JsonProperty]
        public string ExternalProviderId { get; private set; }
        
        /// <summary>
        /// Matching engine ID
        /// </summary>
        [JsonProperty]
        public string MatchingEngineId { get; private set; }
        
        /// <summary>
        /// Legal Entity ID
        /// </summary>
        [JsonProperty]
        public string LegalEntity { get; private set; }
        
        /// <summary>
        /// Force open of new position
        /// </summary>
        [JsonProperty]
        public bool ForceOpen { get; private set; }
        
        /// <summary>
        /// Order type
        /// </summary>
        [JsonProperty]
        public OrderType OrderType { get; private set; }
        
        /// <summary>
        /// ID of parent order (for related orders)
        /// </summary>
        [JsonProperty]
        public string ParentOrderId { get; private set; } 
        
        /// <summary>
        /// ID of parent position (for related orders)
        /// </summary>
        [JsonProperty]
        public string ParentPositionId { get; private set; }

        /// <summary>
        /// Order initiator
        /// </summary>
        [JsonProperty]
        public OriginatorType Originator { get; private set; }
        
        /// <summary>
        /// Matched orders for execution
        /// </summary>
        [JsonProperty]
        public MatchedOrderCollection MatchedOrders { get; private set; }
        
        /// <summary>
        /// Related orders
        /// </summary>
        [JsonProperty]
        public List<RelatedOrderInfo> RelatedOrders { get; private set; }
        
        /// <summary>
        /// Additional information about order, changed every time, when order is changed via user request
        /// </summary>
        [JsonProperty]
        public string AdditionalInfo { get; private set; }

        /// <summary>
        /// Max distance between order price and parent order price (only for trailing order)
        /// </summary>
        [JsonProperty]
        public decimal? TrailingDistance { get; private set; }
        
        /// <summary>
        /// The correlation identifier.
        /// In every operation that results in the creation of a new message the correlationId should be copied from
        /// the inbound message to the outbound message. This facilitates tracking of an operation through the system.
        /// If there is no inbound identifier then one should be created eg. on the service layer boundary (API).  
        /// </summary>
        [JsonProperty]
        public string CorrelationId { get; private set; }
        
        /// <summary>
        /// Order execution rank, calculated based on type and direction
        /// </summary>
        [JsonProperty]
        public byte ExecutionRank { get; private set; }
        
        /// <summary>
        /// Order execution price rank, calculated based on type, direction and price
        /// </summary>
        [JsonProperty]
        public decimal? ExecutionPriceRank { get; private set; }
        
        #endregion

        /// <summary>
        /// For testing and deserialization
        /// </summary>
        [JsonConstructor]
        protected Order()
        {
            MatchedOrders = new MatchedOrderCollection();
            RelatedOrders = new List<RelatedOrderInfo>();
        }

        public Order(string id, long code, string assetPairId, decimal volume,
            DateTime created, DateTime lastModified,
            DateTime? validity, string accountId, string tradingConditionId, string accountAssetId, decimal? price,
            string equivalentAsset, OrderFillType fillType, string comment, string legalEntity, bool forceOpen,
            OrderType orderType, string parentOrderId, string parentPositionId, OriginatorType originator,
            decimal equivalentRate, decimal fxRate, OrderStatus status, string additionalInfo, string correlationId)
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
            Status = status;
            AdditionalInfo = additionalInfo;
            CorrelationId = correlationId;
            ExecutionRank = (byte) (OrderType.GetExecutionRank() | Direction.GetExecutionRank());
            SetExecutionSortRank();
            MatchedOrders = new MatchedOrderCollection();
            RelatedOrders = new List<RelatedOrderInfo>();
        }


        #region Actions

        public void ChangePrice(decimal newPrice, DateTime dateTime, OriginatorType originator, string additionalInfo,
            string correlationId)
        {
            if (OrderType == OrderType.TrailingStop)
            {
                TrailingDistance += Price - newPrice;
            }
            
            LastModified = dateTime;
            Price = newPrice;
            Originator = originator;
            AdditionalInfo = additionalInfo ?? AdditionalInfo;
            CorrelationId = correlationId;
            SetExecutionSortRank();
        }
        
        public void ChangeVolume(decimal newVolume, DateTime dateTime, OriginatorType originator)
        {
            LastModified = dateTime;
            Volume = newVolume;
            Originator = originator;
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

        public void SetTrailingDistance(decimal parentOrderPrice)
        {
            if (OrderType == OrderType.TrailingStop && Price.HasValue)
            {
                TrailingDistance = Price.Value - parentOrderPrice;
            }
        }

        public void StartExecution(DateTime dateTime, string matchingEngineId)
        {
            Status = OrderStatus.ExecutionStarted;
            ExecutionStarted = dateTime;
            LastModified = dateTime;
            MatchingEngineId = matchingEngineId;
        }
        
        public void CancelExecution(DateTime dateTime)
        {
            Status = OrderStatus.Active;
            ExecutionStarted = null;
            LastModified = dateTime;
            MatchingEngineId = null;
        }
        
        public void Execute(DateTime dateTime, MatchedOrderCollection matchedOrders, int assetPairAccuracy)
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
            ExecutionPrice = Math.Round(matchedOrders.WeightedAveragePrice, assetPairAccuracy);
            ExternalOrderId = externalOrderId;
            ExternalProviderId = externalProviderId;
            MatchedOrders.AddRange(matchedOrders);
        }
        
        public void PartiallyExecute(DateTime dateTime, MatchedOrderCollection matchedOrders)
        {
            LastModified = dateTime;
            MatchedOrders.AddRange(matchedOrders);
        }

        public void SetRates(decimal equivalentRate, decimal fxRate)
        {
            EquivalentRate = equivalentRate;
            FxRate = fxRate;
        }

        public void Reject(OrderRejectReason reason, string reasonText, string comment, DateTime dateTime)
        {
            Status = OrderStatus.Rejected;
            Originator = OriginatorType.System;
            RejectReason = reason;
            RejectReasonText = reasonText;
            Comment = comment;
            Rejected = dateTime;
            LastModified = dateTime;
        }

        public void Cancel(DateTime dateTime, OriginatorType originator, string additionalInfo, string correlationId)
        {
            Status = OrderStatus.Canceled;
            Canceled = dateTime;
            LastModified = dateTime;
            AdditionalInfo = additionalInfo ?? AdditionalInfo;
            Originator = originator;
            CorrelationId = correlationId;
        }

        public void Expire(DateTime dateTime)
        {
            Status = OrderStatus.Expired;
            Canceled = dateTime;
            LastModified = dateTime;
            Originator = OriginatorType.System;
        }

        public void AddRelatedOrder(Order order)
        {
            var info = new RelatedOrderInfo {Type = order.OrderType, Id = order.Id};
            
            if (!RelatedOrders.Contains(info))
                RelatedOrders.Add(info);
        }
        
        public void RemoveRelatedOrder(string relatedOrderId)
        {
            var relatedOrder = RelatedOrders.FirstOrDefault(o => o.Id == relatedOrderId);

            if (relatedOrder != null)
                RelatedOrders.Remove(relatedOrder);
        }

        private void SetExecutionSortRank()
        {
            if (Price == null)
                return;

            //for Buy Limit and Sell Stop order should be Desc, to have Asc always, inverse price
            if (OrderType == OrderType.Limit && Direction == OrderDirection.Buy
                ||
                OrderType == OrderType.Stop && Direction == OrderDirection.Sell)
            {
                ExecutionPriceRank = -Price;
            }
            else
            {
                ExecutionPriceRank = Price;    
            }
        }

        #endregion
        
    }
}