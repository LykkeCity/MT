using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services
{
    public interface IValidateOrderService
    {
        void Validate(Position order);
        void ValidateOrderStops(OrderDirection type, BidAskPair quote, decimal deltaBid, decimal deltaAsk, decimal? takeProfit,
            decimal? stopLoss, decimal? expectedOpenPrice, int assetAccuracy);

        void ValidateInstrumentPositionVolume(ITradingInstrument assetPair, Position order);
        Task<Order> ValidateRequestAndGetOrder(OrderPlaceRequest request);
    }
}
