namespace MarginTrading.Common.ClientContracts
{
    public class AccountAssetPairClientContract
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public string Instrument { get; set; }
        public int LeverageInit { get; set; }
        public int LeverageMaintenance { get; set; }
        public decimal SwapLong { get; set; }
        public decimal SwapShort { get; set; }
        public decimal SwapLongPct { get; set; }
        public decimal SwapShortPct { get; set; }
        public decimal CommissionLong { get; set; }
        public decimal CommissionShort { get; set; }
        public decimal CommissionLot { get; set; }
        public decimal DeltaBid { get; set; }
        public decimal DeltaAsk { get; set; }
        public decimal DealLimit { get; set; }
        public decimal PositionLimit { get; set; }
    }
}
