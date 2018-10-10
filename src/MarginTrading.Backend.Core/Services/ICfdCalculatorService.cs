namespace MarginTrading.Backend.Core
{
    //TODO: think about removal of legalEntity from methods (take from instrument)
    public interface ICfdCalculatorService
    {
        decimal GetQuoteRateForBaseAsset(string accountAssetId, string instrument, string legalEntity, 
            bool useAsk);
        decimal GetQuoteRateForQuoteAsset(string accountAssetId, string instrument, string legalEntity, 
            bool metricIsPositive = true);
    }
}
