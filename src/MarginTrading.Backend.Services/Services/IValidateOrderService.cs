using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services
{
    public interface IValidateOrderService
    {
        Task<(Order order, List<Order> relatedOrders)> ValidateRequestAndCreateOrders(OrderPlaceRequest request);

        void MakePreTradeValidation(Order order, bool shouldOpenNewPosition);

        void ValidateOrderPriceChange(Order order, decimal newPrice);
        
        bool ShouldTryExecutePendingOrder(string assetPairId, OrderType orderType, bool shouldOpenNewPosition);
    }
}
