// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.StateMachines;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MarginTrading.Backend.Core.Trading
{
    public class Order : StatefulObject<OrderStatus, OrderCommand>
    {
        private List<string> _positionsToBeClosed;
        private string _parentPositionId;

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
        /// FX asset pair id
        /// </summary>
        [JsonProperty]
        public string FxAssetPairId { get; protected set; }
        
        /// <summary>
        /// Shows if account asset id is directly related on asset pair quote asset.
        /// I.e. AssetPair is {BaseId, QuoteId} and FxAssetPair is {QuoteId, AccountAssetId} => Straight
        /// If AssetPair is {BaseId, QuoteId} and FxAssetPair is {AccountAssetId, QuoteId} => Reverse
        /// </summary>
        [JsonProperty]
        public FxToAssetPairDirection FxToAssetPairDirection { get; protected set; }
        
        /// <summary>
        /// Current order status
        /// </summary>
        [JsonProperty]
        public sealed override OrderStatus Status { get; protected set; }

        public bool IsExecutionNotStarted => Status != OrderStatus.Executed && Status != OrderStatus.ExecutionStarted;

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
        /// ID of external order (for STP mode)
        /// </summary>
        [JsonProperty]
        public string ExternalOrderId { get; private set; }
        
        /// <summary>
        /// ID of external LP (for STP mode)
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
        public string ParentPositionId
        {
            get => _parentPositionId;

            //TODO: remove after version is applied and data is migrated
            private set
            {
                _parentPositionId = value;

                if (string.IsNullOrEmpty(value))
                {
                    return;
                }
                
                if (_positionsToBeClosed == null)
                    _positionsToBeClosed = new List<string>();

                if (!_positionsToBeClosed.Contains(value))
                {
                    _positionsToBeClosed.Add(value);
                }
            }
        }

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

        private string _additionalInfo;
        /// <summary>
        /// Additional information about order, changed every time, when order is changed via user request
        /// </summary>
        [JsonProperty]
        public string AdditionalInfo
        {
            get => _additionalInfo;
            set
            {
                _additionalInfo = value;
                UpdateHasOnBehalf(value);
            }
        }

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

        [JsonProperty]
        public List<string> PositionsToBeClosed
        {
            get => _positionsToBeClosed.Distinct().ToList();

            private set => _positionsToBeClosed = value?.Distinct().ToList() ?? new List<string>();
        }

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
        
        /// <summary>
        /// Number of pending order retries passed
        /// </summary>
        [JsonProperty]
        public int PendingOrderRetriesCount { get; private set; }
        
        /// <summary>
        /// Show if order was managed on behalf at least once
        /// </summary>
        [JsonProperty]
        public bool HasOnBehalf { get; set; }
        
        #endregion
        
        /// <summary>
        /// For testing and deserialization
        /// </summary>
        [JsonConstructor]
        protected Order()
        {
            MatchedOrders = new MatchedOrderCollection();
            RelatedOrders = new List<RelatedOrderInfo>();
            _positionsToBeClosed = new List<string>();
        }

        public Order(string id, long code, string assetPairId, decimal volume,
            DateTime created, DateTime lastModified, DateTime? validity, string accountId, string tradingConditionId, 
            string accountAssetId, decimal? price, string equivalentAsset, OrderFillType fillType, string comment, 
            string legalEntity, bool forceOpen, OrderType orderType, string parentOrderId, string parentPositionId, 
            OriginatorType originator, decimal equivalentRate, decimal fxRate, 
            string fxAssetPairId, FxToAssetPairDirection fxToAssetPairDirection, OrderStatus status, 
            string additionalInfo, string correlationId, List<string> positionsToBeClosed = null, 
            string externalProviderId = null)
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
            FxAssetPairId = fxAssetPairId;
            FxToAssetPairDirection = fxToAssetPairDirection;
            Direction = volume.GetOrderDirection();
            Status = status;
            AdditionalInfo = additionalInfo;
            CorrelationId = correlationId;
            _positionsToBeClosed = positionsToBeClosed?.Distinct().ToList() ?? (string.IsNullOrEmpty(parentPositionId)
                                       ? new List<string>()
                                       : new List<string> {parentPositionId});
            ExternalProviderId = externalProviderId;
            ExecutionRank = (byte) (OrderType.GetExecutionRank() | Direction.GetExecutionRank());
            SetExecutionSortRank();
            MatchedOrders = new MatchedOrderCollection();
            RelatedOrders = new List<RelatedOrderInfo>();
        }

        #region Actions

        public void ChangePrice(decimal newPrice, DateTime dateTime, OriginatorType originator, string additionalInfo,
            string correlationId, bool shouldUpdateTrailingDistance = false)
        {
            if (OrderType == OrderType.TrailingStop && shouldUpdateTrailingDistance)
            {
                TrailingDistance += newPrice - Price;
            }
            
            LastModified = dateTime;
            Price = newPrice;
            Originator = originator;
            AdditionalInfo = additionalInfo ?? AdditionalInfo;
            CorrelationId = correlationId;
            SetExecutionSortRank();
        }
        
        public void ChangeValidity(DateTime? newValidity, DateTime dateTime, OriginatorType originator, string additionalInfo,
            string correlationId)
        {
            LastModified = dateTime;
            Validity = newValidity;
            Originator = originator;
            AdditionalInfo = additionalInfo ?? AdditionalInfo;
            CorrelationId = correlationId;
        }

        public void FixValidity(DateTime? newValidity)
        {
            ChangeValidity(newValidity, LastModified, Originator, AdditionalInfo, CorrelationId);
        }
        
        public void ChangeForceOpen(bool newForceOpen, DateTime dateTime, OriginatorType originator, string additionalInfo,
            string correlationId)
        {
            LastModified = dateTime;
            ForceOpen = newForceOpen;
            Originator = originator;
            AdditionalInfo = additionalInfo ?? AdditionalInfo;
            CorrelationId = correlationId;
        }
        
        public void ChangeVolume(decimal newVolume, DateTime dateTime, OriginatorType originator)
        {
            LastModified = dateTime;
            Volume = newVolume;
            Originator = originator;
        }

        public void SetTrailingDistance(decimal parentOrderPrice)
        {
            if (OrderType == OrderType.TrailingStop && Price.HasValue)
            {
                TrailingDistance = Price.Value - parentOrderPrice;
            }
        }

        public void SetRates(decimal equivalentRate, decimal fxRate)
        {
            EquivalentRate = equivalentRate;
            FxRate = fxRate;
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
        
        public void PartiallyExecute(DateTime dateTime, MatchedOrderCollection matchedOrders)
        {
            LastModified = dateTime;
            MatchedOrders.AddRange(matchedOrders);
        }

        #endregion Actions

        #region State changes

        public void MakeInactive(DateTime dateTime)
        {
            ChangeState(OrderCommand.MakeInactive, () =>
            {
                LastModified = dateTime;
            });
        }
                
        public void Activate(DateTime dateTime, bool relinkFromOrderToPosition, decimal? positionClosePrice)
        {
            ChangeState(OrderCommand.Activate, () =>
            {
                Activated = dateTime;
                LastModified = dateTime;

                if (relinkFromOrderToPosition)
                {
                    ParentPositionId = ParentOrderId;

                    if (!PositionsToBeClosed.Contains(ParentOrderId))
                    {
                        PositionsToBeClosed.Add(ParentOrderId);
                    }
                }

                if (positionClosePrice.HasValue && OrderType == OrderType.TrailingStop)
                {
                    SetTrailingDistance(positionClosePrice.Value);
                }
            });
        }

        public void StartExecution(DateTime dateTime, string matchingEngineId)
        {
            ChangeState(OrderCommand.StartExecution, () =>
            {
                ExecutionStarted = dateTime;
                LastModified = dateTime;
                MatchingEngineId = matchingEngineId;
            });
        }
        
        public void CancelExecution(DateTime dateTime)
        {
            ChangeState(OrderCommand.CancelExecution, () =>
            {
                ExecutionStarted = null;
                LastModified = dateTime;
                MatchingEngineId = null;
                
                PendingOrderRetriesCount++;
            });
        }
        
        public void Execute(DateTime dateTime, MatchedOrderCollection matchedOrders, int assetPairAccuracy)
        {
            ChangeState(OrderCommand.FinishExecution, () =>
            {
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
            });
        }
        
        public void Reject(OrderRejectReason reason, string reasonText, string comment, DateTime dateTime)
        {
            ChangeState(OrderCommand.Reject, () =>
            {
                RejectReason = reason;
                RejectReasonText = reasonText;
                Comment = comment;
                Rejected = dateTime;
                LastModified = dateTime;
            });
        }

        public void Cancel(DateTime dateTime, string additionalInfo, string correlationId)
        {
            ChangeState(OrderCommand.Cancel, () =>
            {
                Canceled = dateTime;
                LastModified = dateTime;
                AdditionalInfo = additionalInfo ?? AdditionalInfo;
                CorrelationId = correlationId;
            });
        }

        public void Expire(DateTime dateTime)
        {
            ChangeState(OrderCommand.Expire, () =>
            {
                Canceled = dateTime;
                LastModified = dateTime;
            });
        }

        #endregion State changes

        #region Helpers

        private void UpdateHasOnBehalf(string additionalInfo)
        {
            HasOnBehalf |= GetOnBehalfFlag(additionalInfo);
        }

        private static bool GetOnBehalfFlag(string additionalInfo)
        {
            if (string.IsNullOrWhiteSpace(additionalInfo))
                return false;

            try
            {
                return JsonConvert.DeserializeAnonymousType(additionalInfo, new {WithOnBehalfFees = false})
                    .WithOnBehalfFees;
            }
            catch
            {
                return false;
            }
        }
        
        #endregion Helpers
    }
}