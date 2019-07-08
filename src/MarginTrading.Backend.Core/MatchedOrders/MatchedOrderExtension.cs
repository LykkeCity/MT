// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core.MatchedOrders
{
    public static class MatchedOrderExtension
    {
        public static LimitOrder CreateLimit(this MatchedOrder order, string instrument, OrderDirection direction)
        {
            return new LimitOrder
            {
                MarketMakerId = order.MarketMakerId,
                Instrument = instrument,
                Price = order.Price,
                Volume = direction == OrderDirection.Buy ? order.LimitOrderLeftToMatch : -order.LimitOrderLeftToMatch
            };
        }
    }
}