namespace MarginTrading.Backend.Core
{
    public interface ICfdCalculatorService
    {
        decimal GetQuoteRateForBaseAsset(string accountAssetId, string instrument, string legalEntity);
        decimal GetQuoteRateForQuoteAsset(string accountAssetId, string instrument, string legalEntity);
        decimal GetVolumeInAccountAsset(OrderDirection direction, string accountAssetId, string instrument,
            decimal volume, string legalEntity);
    }
}
