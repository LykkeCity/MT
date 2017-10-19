using MarginTrading.Common.Mappers;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class OrderbooksBackendResponse
    {
        public OrderBookBackendContract Orderbook { get; set; }

        public static OrderbooksBackendResponse Create(OrderBook orderbook)
        {
            return new OrderbooksBackendResponse
            {
                Orderbook = orderbook.ToBackendContract()
            };
        }
    }
}
