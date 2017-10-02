using System;
using MarginTrading.Core.MatchedOrders;

namespace MarginTrading.Core
{
    public interface IBaseOrder
    {
        string Id { get; }
        string Instrument { get; }
        decimal Volume { get; }
        DateTime CreateDate { get; }
        MatchedOrderCollection MatchedOrders { get; set; }
    }

    public class BaseOrder : IBaseOrder
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Instrument { get; set; }
        public decimal Volume { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public MatchedOrderCollection MatchedOrders { get; set; } = new MatchedOrderCollection();
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

        public static bool GetIsFullfilled(this IBaseOrder order)
        {
            return 0 == Math.Round(order.GetRemainingVolume(), MarginTradingHelpers.VolumeAccuracy);
        }

        public static decimal GetRemainingVolume(this IBaseOrder order)
        {
            return Math.Abs(order.Volume) - order.MatchedOrders.SummaryVolume;
        }
    }
}
