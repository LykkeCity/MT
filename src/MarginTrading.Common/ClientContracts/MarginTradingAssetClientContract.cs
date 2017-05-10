namespace MarginTrading.Common.ClientContracts
{
    public class MarginTradingAssetClientContract
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BaseAssetId { get; set; }
        public string QuoteAssetId { get; set; }
        public int Accuracy { get; set; }
        public int LeverageInit { get; set; }
        public int LeverageMaintenance { get; set; }
        public double DeltaBid { get; set; }
        public double DeltaAsk { get; set; }
        public double SwapLong { get; set; }
        public double SwapShort { get; set; }
        public double SwapLongPct { get; set; }
        public double SwapShortPct { get; set; }
    }
}
