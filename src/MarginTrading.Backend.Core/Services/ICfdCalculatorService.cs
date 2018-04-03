namespace MarginTrading.Backend.Core
{
    public interface ICfdCalculatorService
    {
        decimal GetQuoteRateForBaseAsset(string accountAssetId, string instrument);
        decimal GetQuoteRateForQuoteAsset(string accountAssetId, string instrument);
        decimal GetFplRate(string accountAssetId, string instrumentId, bool fplSign);
        decimal GetSwapRate(string accountAssetId, string instrumentId, bool swapSign);
        decimal GetMarginRate(string accountAssetId, string instrumentId);
        decimal GetVolumeInAccountAsset(OrderDirection direction, string accountAssetId, string instrument, decimal volume);
    }
}
