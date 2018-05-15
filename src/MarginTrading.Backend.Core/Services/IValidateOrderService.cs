using System.Threading.Tasks;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Core
{
    public interface IValidateOrderService
    {
        void Validate(Order order);
        void ValidateOrderStops(OrderDirection type, BidAskPair quote, decimal deltaBid, decimal deltaAsk, decimal? takeProfit,
            decimal? stopLoss, decimal? expectedOpenPrice, int assetAccuracy);

        Task ValidateInstrumentPositionVolume(IAccountAssetPair assetPair, Order order);
    }
}
