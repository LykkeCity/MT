using System;
using MarginTrading.Core;

namespace MarginTrading.Common.ClientContracts
{
    public class OrderHistoryClientContract
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string Instrument { get; set; }
        public int AssetAccuracy { get; set; }
        public OrderDirection Type { get; set; }
        public OrderStatus Status { get; set; }
        public OrderCloseReason CloseReason { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public double OpenPrice { get; set; }
        public double ClosePrice { get; set; }
        public double Volume { get; set; }
        public double? TakeProfit { get; set; }
        public double? StopLoss { get; set; }
        public double Fpl { get; set; }
        public double TotalPnL { get; set; }
        public double PnL { get; set; }
        public double InterestRateSwap { get; set; }
        public double OpenCommission { get; set; }
        public double CloseCommission { get; set; }
    }
}
