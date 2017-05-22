namespace MarginTrading.Common.BackendContracts
{
    public class MarginTradingAssetBackendContract
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BaseAssetId { get; set; }
        public string QuoteAssetId { get; set; }
        public int Accuracy { get; set; }
    }
}
