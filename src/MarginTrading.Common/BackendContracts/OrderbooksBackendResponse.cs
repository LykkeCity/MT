using System.Collections.Generic;
using System.Linq;
using MarginTrading.Common.Mappers;
using MarginTrading.Core;

namespace MarginTrading.Common.BackendContracts
{
    public class OrderbooksBackendResponse
    {
        public Dictionary<string, OrderBookBackendContract> Orderbooks { get; set; }

        public static OrderbooksBackendResponse Create(Dictionary<string, OrderBook> orderbooks)
        {
            return new OrderbooksBackendResponse
            {
                Orderbooks = orderbooks.ToDictionary(pair => pair.Key, pair => pair.Value.ToBackendContract())
            };
        }
    }
}
