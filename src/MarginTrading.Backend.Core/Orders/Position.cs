using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Orders
{
    public class Position
    {
        public string Id { get; private set; }
        public long Code { get; private set; }
        public string AssetPairId { get; private set; }
        public PositionDirection Direction { get; private set; }
        public decimal Volume { get; private set; }
        public string AccountId { get; private set; }
        public string TradingConditionId { get; private set; }
        public string AccountAssetId { get; private set; }
        public decimal? ExpectedOpenPrice { get; private set; }
        public string OpenMatchingEngineId { get; private set; }
        public DateTime OpenDate { get; private set; }
        public string OpenTradeId { get; private set; }
        public decimal OpenPrice { get; private set; }
        public decimal OpenFxPrice { get; private set; }
        public string EquivalentAsset { get; private set; }
        public decimal OpenPriceEquivalent { get; private set; }
        public List<RelatedOrderInfo> RelatedOrders { get; private set; }
        public string LegalEntity { get; private set; }  
        public OriginatorType OpenOriginator { get; private set; }
        public string ExternalProviderId { get; private set; }

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
        public List<string> CloseTrades { get; private set; }
        
        public PositionStatus Status { get; private set; }
        
        public DateTime? LastModified { get; private set; }

        public decimal ChargedPnL { get; private set; }
        
        public HashSet<string> ChargePnlOperations { get; private set; }
        
        public FplData FplData { get; private set; } = new FplData();

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
            CloseOriginator = CloseOriginator ?? originator;
            CloseReason = closeReason;
            CloseComment = comment;
            CloseTrades.Add(tradeId);
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

        public void ChargePnL(string operationId, decimal value)
        {
            //if operation was already processed - it is duplicated event
            if (ChargePnlOperations.Contains(operationId))
                return;

            ChargePnlOperations.Add(operationId);
            ChargedPnL += value;
        }
    }
}