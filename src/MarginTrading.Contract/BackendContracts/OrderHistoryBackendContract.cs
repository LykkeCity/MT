using System;

namespace MarginTrading.Contract.BackendContracts
{
    public class OrderHistoryBackendContract
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string Instrument { get; set; }
        public int AssetAccuracy { get; set; }
        public OrderDirectionContract Type { get; set; }
        public OrderStatusContract Status { get; set; }
        public OrderCloseReasonContract CloseReason { get; set; }
        public DateTime? OpenDate { get; set; }
        public DateTime? CloseDate { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal Volume { get; set; }
        public decimal? TakeProfit { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal TotalPnl { get; set; }
        public decimal Pnl { get; set; }
        public decimal InterestRateSwap { get; set; }
        public decimal CommissionLot { get; set; }
        public decimal OpenCommission { get; set; }
        public decimal CloseCommission { get; set; }
        public string EquivalentAsset { get; set; }
        public decimal OpenPriceEquivalent{ get; set; }
        public decimal ClosePriceEquivalent { get; set; }
    }
}
