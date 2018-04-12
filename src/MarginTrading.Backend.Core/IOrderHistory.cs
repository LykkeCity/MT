using System;
using System.Collections.Generic;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;

namespace MarginTrading.Backend.Core
{
    public interface IOrderHistory
    {
        string Id { get; }
        string ClientId { get; }
        string AccountId { get; }
        string TradingConditionId { get; }
        string AccountAssetId { get; }
        string Instrument { get; }
        OrderDirection Type { get; }
        DateTime CreateDate { get; }
        DateTime? OpenDate { get; }
        DateTime? CloseDate { get; }
        decimal? ExpectedOpenPrice { get; }
        decimal OpenPrice { get; }
        decimal ClosePrice { get; }
        decimal QuoteRate { get; }
        int AssetAccuracy { get; }
        decimal Volume { get; }
        decimal? TakeProfit { get; }
        decimal? StopLoss { get; }
        decimal CommissionLot { get; }
        decimal OpenCommission { get; }
        decimal CloseCommission { get; }
        decimal SwapCommission { get; }
        string EquivalentAsset { get; }
        decimal OpenPriceEquivalent{ get; }
        decimal ClosePriceEquivalent { get; }
        DateTime? StartClosingDate { get; }
        OrderStatus Status { get; }
        OrderCloseReason CloseReason { get; }
        OrderFillType FillType { get; }
        OrderRejectReason RejectReason { get; }
        string RejectReasonText { get; }
        string Comment { get; }
        List<MatchedOrder> MatchedOrders { get; }
        List<MatchedOrder> MatchedCloseOrders { get; }

        decimal MatchedVolume { get; }
        decimal MatchedCloseVolume { get; }
        decimal Fpl { get; }
        decimal PnL { get; }
        decimal InterestRateSwap { get; }
        decimal MarginInit { get; }
        decimal MarginMaintenance { get; }
        
        OrderUpdateType OrderUpdateType { get; }
        
        string OpenExternalOrderId { get; }
        string OpenExternalProviderId { get; }
        string CloseExternalOrderId { get; }
        string CloseExternalProviderId { get; }
        
        MatchingEngineMode MatchingEngineMode { get; }
        string LegalEntity { get; set; }  
    }

    public class OrderHistory : IOrderHistory
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public string TradingConditionId { get; set; }
        public string AccountAssetId { get; set; }
        public string Instrument { get; set; }
        public OrderDirection Type { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public decimal? ExpectedOpenPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal QuoteRate { get; set; }
        public int AssetAccuracy { get; set; }
        public decimal Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal CommissionLot { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
        public decimal SwapCommission { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal OpenPriceEquivalent{ get; set; }
        public decimal ClosePriceEquivalent { get; set; }
        public DateTime? StartClosingDate { get; set; }
        public OrderStatus Status { get; set; }
        public OrderCloseReason CloseReason { get; set; }
        public OrderFillType FillType { get; set; }
        public OrderRejectReason RejectReason { get; set; }
        public string RejectReasonText { get; set; }
        public string Comment { get; set; }
        public List<MatchedOrder> MatchedOrders { get; set; } = new List<MatchedOrder>();
        public List<MatchedOrder> MatchedCloseOrders { get; set; } = new List<MatchedOrder>();
        public decimal MatchedVolume { get; set; }
        public decimal MatchedCloseVolume { get; set; }
        public decimal Fpl { get; set; }
        public decimal PnL { get; set; }
        public decimal InterestRateSwap { get; set; }
        public decimal MarginInit { get; set; }
        public decimal MarginMaintenance { get; set; }
        public OrderUpdateType OrderUpdateType { get; set; }
        public string OpenExternalOrderId { get; set; }
        public string OpenExternalProviderId { get; set; }
        public string CloseExternalOrderId { get; set; }
        public string CloseExternalProviderId { get; set; }
        public MatchingEngineMode MatchingEngineMode { get; set; }
        public string LegalEntity { get; set; }
    }
}