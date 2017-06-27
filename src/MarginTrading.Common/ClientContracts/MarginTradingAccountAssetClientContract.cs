namespace MarginTrading.Common.ClientContracts
{
    public class MarginTradingAccountAssetClientContract
    {
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public string Instrument { get; set; }
        public int LeverageInit { get; set; }
        public int LeverageMaintenance { get; set; }
        public double SwapLong { get; set; }
        public double SwapShort { get; set; }
        public double SwapLongPct { get; set; }
        public double SwapShortPct { get; set; }
        public double CommissionLong { get; set; }
        public double CommissionShort { get; set; }
        public double CommissionLot { get; set; }
        public double DeltaBid { get; set; }
        public double DeltaAsk { get; set; }
        public double DealLimit { get; set; }
        public double PositionLimit { get; set; }
    }
}
