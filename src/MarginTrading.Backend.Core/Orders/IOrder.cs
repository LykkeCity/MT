using System;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core.Orders
{
    public interface IOrder : IBaseOrder
    {
        long Code { get; set; }
        string AccountId { get; }
        string TradingConditionId { get; }
        string AccountAssetId { get; }
        
        //Matching Engine ID used for open
        string OpenOrderbookId { get; }
        
        //Matching Engine ID used for close
        string CloseOrderbookId { get; }
        
        DateTime? OpenDate { get; }
        DateTime? CloseDate { get; }
        decimal? ExpectedOpenPrice { get; }
        decimal OpenPrice { get; }
        decimal ClosePrice { get; }
        decimal? TakeProfit { get; }
        decimal? StopLoss { get; }
        decimal OpenCommission { get; }
        decimal CloseCommission { get; }
        decimal CommissionLot { get; }
        decimal QuoteRate { get; }
        int AssetAccuracy { get; }
        DateTime? StartClosingDate { get; }
        OrderStatus Status { get; }
        OrderCloseReason CloseReason { get; }
        OrderFillType FillType { get; }
        OrderRejectReason RejectReason { get; }
        string CloseRejectReasonText { get; }
        string RejectReasonText { get; }
        string Comment { get; }
        MatchedOrderCollection MatchedCloseOrders { get; }
        decimal SwapCommission { get; }
        string MarginCalcInstrument { get; }
        string EquivalentAsset { get; }
        decimal OpenPriceEquivalent { get; }
        decimal ClosePriceEquivalent { get; }
        
        #region Extenal orders matching
        
        string OpenExternalOrderId { get; }
        
        string OpenExternalProviderId { get; }
        
        string CloseExternalOrderId { get; }
        
        string CloseExternalProviderId { get; }
        
        #endregion
        
        MatchingEngineMode MatchingEngineMode { get; }
        
        string LegalEntity { get; }
        
        bool ForceOpen { get; }
        
        OrderType OrderType { get; }
        
        string ParentOrderId { get; } 
        
        string ParentPositionId { get; }
        
        DateTime? Validity { get; }
        
        OriginatorType Originator { get; }
        DateTime? LastModified { get; set; }
    }
}
