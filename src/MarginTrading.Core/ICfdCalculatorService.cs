namespace MarginTrading.Core
{
    public interface ICfdCalculatorService
    {
        decimal GetQuoteRateForBaseAsset(string accountAssetId, string instrument);
        decimal GetQuoteRateForQuoteAsset(string accountAssetId, string instrument);
        decimal GetVolumeInAccountAsset(OrderDirection direction, string accountAssetId, string instrument, decimal volume);
    }
}
