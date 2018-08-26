namespace MarginTrading.Backend.Core
{
    public interface ICfdCalculatorService
    {
        decimal GetQuoteRateForBaseAsset(string accountAssetId, string instrument, string legalEntity, 
            bool isBuy);
        decimal GetQuoteRateForQuoteAsset(string accountAssetId, string instrument, string legalEntity, 
            bool metricIsPositive = true);
    }
}
