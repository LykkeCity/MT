namespace MarginTrading.Core
{
    public interface ICfdCalculatorService
    {
        double GetQuoteRateForBaseAsset(string accountAssetId, string instrument);
        double GetQuoteRateForQuoteAsset(string accountAssetId, string instrument);
        double GetVolumeInAccountAsset(OrderDirection direction, string accountAssetId, string instrument, double volume);
    }
}
