using System;
using System.Collections.Generic;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class OrderFullContract
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
        public double CommissionLot { get; set; }
        public double SwapCommission { get; set; }
        public DateTime? StartClosingDate { get; set; }
        public OrderStatus Status { get; set; }
        public OrderCloseReason CloseReason { get; set; }
        public OrderFillType FillType { get; set; }
        public OrderRejectReason RejectReason { get; set; }
        public string RejectReasonText { get; set; }
        public string Comment { get; set; }
        public List<MatchedOrderBackendContract> MatchedOrders { get; set; } = new List<MatchedOrderBackendContract>();
        public List<MatchedOrderBackendContract> MatchedCloseOrders { get; set; } = new List<MatchedOrderBackendContract>();

        public double MatchedVolume { get; set; }
        public double MatchedCloseVolume { get; set; }
        public double TotalPnL { get; set; }
        public double PnL { get; set; }
        public double InterestRateSwap { get; set; }
        public double MarginInit { get; set; }
        public double MarginMaintenance { get; set; }
        public double OpenCrossPrice { get; set; }
        public double CloseCrossPrice { get; set; }
    }
}
