namespace MarginTrading.Backend.Core
{
    public interface IAssetPair
    {
        string Id { get; }
        string Name { get; }
        string BaseAssetId { get; }
        string QuoteAssetId { get; }
        int Accuracy { get; }
    }

    public class AssetPair : IAssetPair
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string BaseAssetId { get; set; }
        public string QuoteAssetId { get; set; }
        public int Accuracy { get; set; }
    }
}
