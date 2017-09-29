using System;
using System.Collections.Generic;
using MarginTrading.Core.MatchedOrders;

namespace MarginTrading.Core
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
        double? ExpectedOpenPrice { get; }
        double OpenPrice { get; }
        double ClosePrice { get; }
        double QuoteRate { get; }
        int AssetAccuracy { get; }
        double Volume { get; }
        double? TakeProfit { get; }
        double? StopLoss { get; }
        double OpenCommission { get; }
        double CloseCommission { get; }
        double SwapCommission { get; }
        DateTime? StartClosingDate { get; }
        OrderStatus Status { get; }
        OrderCloseReason CloseReason { get; }
        OrderFillType FillType { get; }
        OrderRejectReason RejectReason { get; }
        string RejectReasonText { get; }
        string Comment { get; }
        List<MatchedOrder> MatchedOrders { get; }
        List<MatchedOrder> MatchedCloseOrders { get; }

        double MatchedVolume { get; }
        double MatchedCloseVolume { get; }
        double Fpl { get; }
        double PnL { get; }
        double InterestRateSwap { get; }
        double MarginInit { get; }
        double MarginMaintenance { get; }
        double OpenCrossPrice { get; }
        double CloseCrossPrice { get; }
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
        public double? ExpectedOpenPrice { get; set; }
        public double OpenPrice { get; set; }
        public double ClosePrice { get; set; }
        public double QuoteRate { get; set; }
        public int AssetAccuracy { get; set; }
        public double Volume { get; set; }
        public double? TakeProfit { get; set; }
        public double? StopLoss { get; set; }
        public double OpenCommission { get; set; }
        public double CloseCommission { get; set; }
        public double SwapCommission { get; set; }
        public DateTime? StartClosingDate { get; set; }
        public OrderStatus Status { get; set; }
        public OrderCloseReason CloseReason { get; set; }
        public OrderFillType FillType { get; set; }
        public OrderRejectReason RejectReason { get; set; }
        public string RejectReasonText { get; set; }
        public string Comment { get; set; }
        public List<MatchedOrder> MatchedOrders { get; set; } = new List<MatchedOrder>();
        public List<MatchedOrder> MatchedCloseOrders { get; set; } = new List<MatchedOrder>();
        public double MatchedVolume { get; set; }
        public double MatchedCloseVolume { get; set; }
        public double Fpl { get; set; }
        public double PnL { get; set; }
        public double InterestRateSwap { get; set; }
        public double MarginInit { get; set; }
        public double MarginMaintenance { get; set; }
        public double OpenCrossPrice { get; set; }
        public double CloseCrossPrice { get; set; }
    }
}