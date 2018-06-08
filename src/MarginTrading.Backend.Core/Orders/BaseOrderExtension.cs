using System;

namespace MarginTrading.Backend.Core.Orders
{
    public static class BaseOrderExtension
    {
        public static OrderDirection GetOrderDirection(this IBaseOrder order)
        {
            return order.Volume >= 0 ? OrderDirection.Buy : OrderDirection.Sell;
        }

        public static OrderDirection GetCloseType(this IBaseOrder order)
        {
            return order.Volume >= 0 ? OrderDirection.Sell : OrderDirection.Buy;
        }

        public static bool GetIsFullfilled(this IBaseOrder order)
        {
            return 0 == Math.Round(order.GetRemainingVolume(), MarginTradingHelpers.VolumeAccuracy);
        }

        public static decimal GetRemainingVolume(this IBaseOrder order)
        {
            return Math.Round(Math.Abs(order.Volume) - order.MatchedOrders.SummaryVolume,
                MarginTradingHelpers.VolumeAccuracy);
        }
    }
}