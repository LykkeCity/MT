using System;
using System.Collections.Generic;
using System.Data;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core.Orders
{
    public class Position
    {
        public string Id { get; }
        public long Code { get; }
        public string AssetPairId { get; }
        public PositionDirection Direction { get; }
        public decimal Volume { get; }
        public string AccountId { get; }
        public string TradingConditionId { get; }
        public string AccountAssetId { get; }
        public int AssetPairAccuracy { get; }
        public decimal? ExpectedOpenPrice { get; }
        public string OpenMatchingEngineId { get; }
        public DateTime OpenDate { get; }
        public string OpenTradeId { get; }
        public decimal OpenPrice { get; }
        public string EquivalentAsset { get; }
        public decimal OpenPriceEquivalent { get; }
        public List<RelatedOrderInfo> RelatedOrders { get; }
        public string LegalEntity { get; }  
        public OriginatorType OpenOriginator { get; }
        public string ExternalProviderId { get; }

        public decimal SwapCommission { get; private set; }
        public decimal OpenCommission { get; private set; }
        public decimal CloseCommission { get; private set; }
        public decimal CommissionLot { get; private set; }
        
        public string CloseMatchingEngineId { get; private set; }
        public decimal ClosePrice { get; private set; }
        public decimal ClosePriceEquivalent { get; private set; }
        public DateTime? StartClosingDate { get; private set; }
        public DateTime? CloseDate { get; private set; }
        public OriginatorType CloseOriginator { get; private set; }
        public PositionCloseReason CloseReason { get; private set; }
        public string CloseComment { get; private set; }
        public List<string> CloseTrades { get; }
        
        public PositionStatus Status { get; private set; }
        
        public DateTime? LastModified { get; private set; }

        public FplData FplData { get; set; } = new FplData();

        public void StartClosing(DateTime date, PositionCloseReason reason, OriginatorType originator, string comment)
        {
            Status = PositionStatus.Closing;
            LastModified = date;
            StartClosingDate = date;
            CloseReason = reason;
            CloseOriginator = originator;
            CloseComment = comment;
        }

        public void UpdateClosePrice(decimal closePrice)
        {
            ClosePrice = closePrice;
            FplData.ActualHash++;
            var account = MtServiceLocator.AccountsCacheService.Get(AccountId);
            account.CacheNeedsToBeUpdated();
        } 
    }
}