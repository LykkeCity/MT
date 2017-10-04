namespace MarginTrading.Core
{
    public interface IValidateOrderService
    {
        void Validate(Order order);
        void ValidateOrderStops(OrderDirection type, BidAskPair quote, decimal deltaBid, decimal deltaAsk, decimal? takeProfit,
            decimal? stopLoss, decimal? expectedOpenPrice, int assetAccuracy);

        void ValidateInstrumentPositionVolume(IAccountAssetPair assetPair, Order order);
    }
}
