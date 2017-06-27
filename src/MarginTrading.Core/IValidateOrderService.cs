namespace MarginTrading.Core
{
    public interface IValidateOrderService
    {
        void Validate(Order order);
        void ValidateOrderStops(OrderDirection type, BidAskPair quote, double deltaBid, double deltaAsk, double? takeProfit,
            double? stopLoss, double? expectedOpenPrice, int assetAccuracy);

        void ValidateInstrumentPositionVolume(IMarginTradingAccountAsset asset, Order order);
    }
}
