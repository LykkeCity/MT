using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Orders
{
    public class Position
    {
        public string Id { get; }
        public long Code { get; }
        public string AssetPairId { get; }
        public PositionDirection Direction { get; }
        public decimal Volume { get; private set; }
        public string AccountId { get; }
        public string TradingConditionId { get; }
        public string AccountAssetId { get; }
        public decimal? ExpectedOpenPrice { get; }
        public string OpenMatchingEngineId { get; }
        public DateTime OpenDate { get; }
        public string OpenTradeId { get; }
        public decimal OpenPrice { get; }
        public decimal OpenFxPrice { get; }
        public string EquivalentAsset { get; }
        public decimal OpenPriceEquivalent { get; }
        public List<RelatedOrderInfo> RelatedOrders { get; }
        public string LegalEntity { get; }  
        public OriginatorType OpenOriginator { get; }
        public string ExternalProviderId { get; }

        public decimal SwapCommissionRate { get; private set; }
        public decimal OpenCommissionRate { get; private set; }
        public decimal CloseCommissionRate { get; private set; }
        public decimal CommissionLot { get; private set; }
        
        public string CloseMatchingEngineId { get; private set; }
        public decimal ClosePrice { get; private set; }
        public decimal CloseFxPrice { get; private set; }
        public decimal ClosePriceEquivalent { get; private set; }
        public DateTime? StartClosingDate { get; private set; }
        public DateTime? CloseDate { get; private set; }
        public OriginatorType? CloseOriginator { get; private set; }
        public PositionCloseReason CloseReason { get; private set; }
        public string CloseComment { get; private set; }
        public List<string> CloseTrades { get; }
        
        public PositionStatus Status { get; private set; }
        
        public DateTime? LastModified { get; private set; }

        public FplData FplData { get; } = new FplData();

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
            ClosePrice = closePrice;
            CloseFxPrice = closeFxPrice;
            ClosePriceEquivalent = closePriceEquivalent;
            CloseOriginator = originator;
            CloseReason = closeReason;
            CloseComment = comment;
            CloseTrades.Add(tradeId);
        }

        public void PartiallyClose(DateTime date, decimal closedVolume, string tradeId)
        {
            LastModified = date;
            Volume = Volume > 0 ? Volume - closedVolume : Volume + closedVolume;
            CloseTrades.Add(tradeId);
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
    }
}