using System;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.Core
{
    public interface IBaseOrder
    {
        string Id { get; }
        string Instrument { get; }
        double Volume { get; }
        DateTime CreateDate { get; }
        List<MatchedOrder> MatchedOrders { get; }
    }

    public class BaseOrder : IBaseOrder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Instrument { get; set; }
        public double Volume { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public List<MatchedOrder> MatchedOrders { get; set; } = new List<MatchedOrder>();
    }

    public static class BaseOrderExtension
    {
        public static OrderDirection GetOrderType(this IBaseOrder order)
        {
            return order.Volume >= 0 ? OrderDirection.Buy : OrderDirection.Sell;
        }

        public static OrderDirection GetCloseType(this IBaseOrder order)
        {
            return order.Volume >= 0 ? OrderDirection.Sell : OrderDirection.Buy;
        }

        public static double GetRemainingVolume(this IBaseOrder order)
        {
            return order.MatchedOrders.Count > 0
                ? Math.Abs(order.Volume) - order.MatchedOrders.Sum(item => item.Volume)
                : Math.Abs(order.Volume);
        }

        public static bool GetIsFullfilled(this IBaseOrder order)
        {
            return 0 == Math.Round(order.GetRemainingVolume(), MarginTradingHelpers.VolumeAccuracy);
        }
    }
}
