using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Backend.Core.Orderbooks
{
    public static class OrderBookExt
    {
        public static AggregatedOrderInfo Aggregate(this OrderBook src, OrderDirection direction)
        {
            var price = direction == OrderDirection.Buy
                ? src.Buy.Keys.FirstOrDefault()
                : src.Sell.Keys.FirstOrDefault();

            decimal volume = direction == OrderDirection.Buy
                ? src.Buy.Values.FirstOrDefault()?.Sum(item => item.Volume) ?? 0
                : src.Sell.Values.FirstOrDefault()?.Sum(item => item.Volume) ?? 0;

            return new AggregatedOrderInfo
            {
                Price = price,
                Volume = volume,
                IsBuy = direction == OrderDirection.Buy
            };
        }

        public static List<LimitOrder> DeleteMarketMakerOrders(this SortedDictionary<decimal, List<LimitOrder>> src,
            string marketMakerId, string[] idsToDelete)
        {
            var result = new List<LimitOrder>();

            foreach (List<LimitOrder> limitOrders in src.Values)
            {
                result.AddRange(limitOrders.Where(x => x.MarketMakerId == marketMakerId && (idsToDelete == null || idsToDelete.Contains(x.Id))));
                limitOrders.RemoveAll(x => x.MarketMakerId == marketMakerId && (idsToDelete == null || idsToDelete.Contains(x.Id)));
            }

            src.RemoveEmptyKeys();

            return result;
        }

        public static List<LimitOrder> DeleteAllOrdersByMarketMaker(this SortedDictionary<decimal, List<LimitOrder>> src,
            string marketMakerId)
        {
            var result = new List<LimitOrder>();

            foreach (List<LimitOrder> limitOrders in src.Values)
            {
                result.AddRange(limitOrders.Where(x => x.MarketMakerId == marketMakerId));
                limitOrders.RemoveAll(x => x.MarketMakerId == marketMakerId);
            }

            src.RemoveEmptyKeys();

            return result;
        }

        public static LimitOrder AddMarketMakerOrder(this SortedDictionary<decimal, List<LimitOrder>> src,
            LimitOrder order)
        {
            if (!src.ContainsKey(order.Price))
                src.Add(order.Price, new List<LimitOrder>());

            var existingOrder = src[order.Price].FirstOrDefault(
                item => item.MarketMakerId == order.MarketMakerId);

            if (existingOrder != null)
            {
                existingOrder.Volume = order.Volume;
                return existingOrder;
            }

            src[order.Price].Add(order);
            return order;
        }

        public static void RemoveEmptyKeys(this SortedDictionary<decimal, List<LimitOrder>> src)
        {
            var emptyKeys = src.Where(pair => pair.Value.Count == 0)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in emptyKeys)
            {
                src.Remove(key);
            }
        }
    }
}