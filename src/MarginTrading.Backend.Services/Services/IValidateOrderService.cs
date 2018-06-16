using System.Collections.Generic;
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
        void ValidateOrderStops(OrderDirection type, BidAskPair quote, decimal deltaBid, decimal deltaAsk, decimal? takeProfit,
            decimal? stopLoss, decimal? expectedOpenPrice, int assetAccuracy);

        Task<(Order order, List<Order> relatedOrders)> ValidateRequestAndGetOrders(OrderPlaceRequest request);

        void MakePreTradeValidation(Order order);
    }
}
