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
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal Fpl { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal PnL { get; set; }
        public decimal InterestRateSwap { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
    }
}
