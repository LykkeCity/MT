namespace MarginTrading.Backend.Core
{
    public interface ICfdCalculatorService
    {
        decimal GetQuoteRateForBaseAsset(string accountAssetId, string instrument, string legalEntity);
        decimal GetQuoteRateForQuoteAsset(string accountAssetId, string instrument, string legalEntity);
        decimal GetFplRate(string accountAssetId, string instrumentId, bool fplSign);
        decimal GetSwapRate(string accountAssetId, string instrumentId, bool swapSign);
        decimal GetMarginRate(string accountAssetId, string instrumentId);
        decimal GetVolumeInAccountAsset(OrderDirection direction, string accountAssetId, string instrument,
            decimal volume, string legalEntity);
    }
}
