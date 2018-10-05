using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Trading;
using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.Orders
{
    public class Position
    {
        [JsonProperty]
        public virtual string Id { get; protected set; }
        [JsonProperty]
        public long Code { get; private set; }
        [JsonProperty]
        public virtual string AssetPairId { get; protected set; }
        [JsonProperty]
        public PositionDirection Direction { get; private set; }
        [JsonProperty]
        public decimal Volume { get; private set; }
        [JsonProperty]
        public virtual string AccountId { get; protected set; }
        [JsonProperty]
        public string TradingConditionId { get; private set; }
        [JsonProperty]
        public string AccountAssetId { get; private set; }
        [JsonProperty]
        public decimal? ExpectedOpenPrice { get; private set; }
        [JsonProperty]
        public string OpenMatchingEngineId { get; private set; }
        [JsonProperty]
        public DateTime OpenDate { get; private set; }
        [JsonProperty]
        public string OpenTradeId { get; private set; }
        [JsonProperty]
        public decimal OpenPrice { get; private set; }
        [JsonProperty]
        public decimal OpenFxPrice { get; private set; }
        [JsonProperty]
        public string EquivalentAsset { get; private set; }
        [JsonProperty]
        public decimal OpenPriceEquivalent { get; private set; }
        [JsonProperty]
        public List<RelatedOrderInfo> RelatedOrders { get; private set; }
        [JsonProperty]
        public string LegalEntity { get; private set; }  
        [JsonProperty]
        public OriginatorType OpenOriginator { get; private set; }
        [JsonProperty]
        public string ExternalProviderId { get; private set; }

        [JsonProperty]
        public decimal SwapCommissionRate { get; private set; }
        [JsonProperty]
        public decimal OpenCommissionRate { get; private set; }
        [JsonProperty]
        public decimal CloseCommissionRate { get; private set; }
        [JsonProperty]
        public decimal CommissionLot { get; private set; }
        
        [JsonProperty]
        public string CloseMatchingEngineId { get; private set; }
        [JsonProperty]
        public decimal ClosePrice { get; private set; }
        [JsonProperty]
        public decimal CloseFxPrice { get; private set; }
        [JsonProperty]
        public decimal ClosePriceEquivalent { get; private set; }
        [JsonProperty]
        public DateTime? StartClosingDate { get; private set; }
        [JsonProperty]
        public DateTime? CloseDate { get; private set; }
        [JsonProperty]
        public OriginatorType? CloseOriginator { get; private set; }
        [JsonProperty]
        public PositionCloseReason CloseReason { get; private set; }
        [JsonProperty]
        public string CloseComment { get; private set; }
        [JsonProperty]
        public List<string> CloseTrades { get; private set; }
        
        [JsonProperty]
        public PositionStatus Status { get; private set; }
        
        [JsonProperty]
        public DateTime? LastModified { get; private set; }

        [JsonProperty]
        public decimal ChargedPnL { get; private set; }
        
        [JsonProperty]
        public virtual HashSet<string> ChargePnlOperations { get; protected set; }

        [JsonProperty]
        public FplData FplData { get; private set; }

        /// <summary>
        /// For testing and deserialization
        /// </summary>
        [JsonConstructor]
        public Position()
        {
            FplData = new FplData {ActualHash = 1};
        }

        public Position(string id, long code, string assetPairId, decimal volume, string accountId,
            string tradingConditionId, string accountAssetId, decimal? expectedOpenPrice,
            string openMatchingEngineId, DateTime openDate, string openTradeId, decimal openPrice, decimal openFxPrice,
            string equivalentAsset, decimal openPriceEquivalent, List<RelatedOrderInfo> relatedOrders,
            string legalEntity, OriginatorType openOriginator, string externalProviderId)
        {
            Id = id;
            Code = code;
            AssetPairId = assetPairId;
            Volume = volume;
            Direction = volume.GetPositionDirection();
            AccountId = accountId;
            TradingConditionId = tradingConditionId;
            AccountAssetId = accountAssetId;
            ExpectedOpenPrice = expectedOpenPrice;
            OpenMatchingEngineId = openMatchingEngineId;
            OpenDate = openDate;
            OpenTradeId = openTradeId;
            OpenPrice = openPrice;
            OpenFxPrice = openFxPrice;
            EquivalentAsset = equivalentAsset;
            OpenPriceEquivalent = openPriceEquivalent;
            RelatedOrders = relatedOrders;
            LegalEntity = legalEntity;
            OpenOriginator = openOriginator;
            ExternalProviderId = externalProviderId;
            CloseTrades = new List<string>();
            ChargePnlOperations = new HashSet<string>();
            FplData = new FplData {ActualHash = 1};
        }

        public void StartClosing(DateTime date, PositionCloseReason reason, OriginatorType originator, string comment)
        {
            Status = PositionStatus.Closing;
            LastModified = date;
            StartClosingDate = date;
            CloseReason = reason;
            CloseOriginator = originator;
            CloseComment = comment;
        }
        
        public void CancelClosing(DateTime date, PositionCloseReason reason, OriginatorType originator, string comment)
        {
            Status = PositionStatus.Active;
            LastModified = date;
            StartClosingDate = null;
            CloseReason = PositionCloseReason.None;
            CloseOriginator = null;
            CloseComment = null;
        }

        public void Close(DateTime date, string closeMatchingEngineId, decimal closePrice, decimal closeFxPrice,
            decimal closePriceEquivalent, OriginatorType originator, PositionCloseReason closeReason, string comment,
            string tradeId)
        {
            Status = PositionStatus.Closed;
            CloseDate = date;
            LastModified = date;
            CloseMatchingEngineId = closeMatchingEngineId;
            CloseFxPrice = closeFxPrice;
            ClosePriceEquivalent = closePriceEquivalent;
            CloseOriginator = CloseOriginator ?? originator;
            CloseReason = closeReason;
            CloseComment = comment;
            CloseTrades.Add(tradeId);
            UpdateClosePrice(closePrice);
        }

        public void PartiallyClose(DateTime date, decimal closedVolume, string tradeId, decimal chargedPnl)
        {
            LastModified = date;
            Volume = Volume > 0 ? Volume - closedVolume : Volume + closedVolume;
            CloseTrades.Add(tradeId);
            ChargedPnL -= chargedPnl;
        }

        public void UpdateClosePrice(decimal closePrice)
        {
            ClosePrice = closePrice;
            FplData.ActualHash++;
            var account = MtServiceLocator.AccountsCacheService.Get(AccountId);
            account.CacheNeedsToBeUpdated();
        }

        public void SetCommissionRates(decimal swapComissionRate, decimal openComissionRate, decimal closeComissionRate,
            decimal comissionLot)
        {
            SwapCommissionRate = swapComissionRate;
            OpenCommissionRate = openComissionRate;
            CloseCommissionRate = closeComissionRate;
            CommissionLot = comissionLot;
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

        public virtual void ChargePnL(string operationId, decimal value)
        {
            //if operation was already processed - it is duplicated event
            if (ChargePnlOperations.Contains(operationId))
                return;

            ChargePnlOperations.Add(operationId);
            ChargedPnL += value;
        }
    }
}