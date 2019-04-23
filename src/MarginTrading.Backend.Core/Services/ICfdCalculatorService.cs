using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    //TODO: think about removal of legalEntity from methods (take from instrument)
    public interface ICfdCalculatorService
    {
        decimal GetQuoteRateForBaseAsset(string accountAssetId, string instrument, string legalEntity, 
            bool useAsk);
        
        decimal GetQuoteRateForQuoteAsset(string accountAssetId, string instrument, string legalEntity, 
            bool metricIsPositive = true);
        
        decimal GetPrice(InstrumentBidAskPair quote, FxToAssetPairDirection direction, 
            bool metricIsPositive = true);

        (string id, FxToAssetPairDirection direction) GetFxAssetPairIdAndDirection(string accountAssetId,
            string assetPairId,
            string legalEntity);
    }
}
