// Copyright (c) 2019 Lykke Corp.

using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Backend.Core.Orderbooks
{
    public static class OrderBookExt
    {
        public static List<LimitOrder> DeleteMarketMakerOrders(this SortedDictionary<decimal, List<LimitOrder>> src,
            string marketMakerId, string[] idsToDelete)
        {
            var result = new List<LimitOrder>();

            foreach (var limitOrders in src.Values)
            {
                result.AddRange(limitOrders.Where(x => x.MarketMakerId == marketMakerId && (idsToDelete == null || idsToDelete.Contains(x.Id))));
                limitOrders.RemoveAll(x => x.MarketMakerId == marketMakerId && (idsToDelete == null || idsToDelete.Contains(x.Id)));
            }

            src.RemoveEmptyKeys();

            return result;
        }

        public static void DeleteAllOrdersByMarketMaker(this SortedDictionary<decimal, List<LimitOrder>> src,
            string marketMakerId)
        {
            foreach (var limitOrders in src.Values)
            {
                limitOrders.RemoveAll(x => x.MarketMakerId == marketMakerId);
            }

            src.RemoveEmptyKeys();
        }

        public static void RemoveEmptyKeys(this SortedDictionary<decimal, List<LimitOrder>> src)
        {
            var emptyItems = src.Where(pair => pair.Value.Count == 0).ToArray();

            foreach (var item in emptyItems)
            {
                src.Remove(item.Key);
            }
        }
    }
}