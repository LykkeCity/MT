using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MarginTrading.Core
{
    public interface IBaseOrder
    {
        string Id { get; }
        string Instrument { get; }
        double Volume { get; }
        DateTime CreateDate { get; }
        IReadOnlyList<MatchedOrder> MatchedOrders { get; set; }
        double RemainingVolume { get; }
    }

    public class BaseOrder : IBaseOrder
    {
        private IReadOnlyList<MatchedOrder> _matchedOrders;
        private double? _remainingVolume;

        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Instrument { get; set; }
        public double Volume { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        public IReadOnlyList<MatchedOrder> MatchedOrders
        {
            get => _matchedOrders ?? new List<MatchedOrder>();
            set
            {
                _matchedOrders = value;

                _remainingVolume = _matchedOrders?.Count > 0
                    ? Math.Abs(Volume) - _matchedOrders.Sum(item => item.Volume)
                    : Math.Abs(Volume);
            }
        }

        public double RemainingVolume => _remainingVolume ?? Math.Abs(Volume);
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
            return 0 == Math.Round(order.RemainingVolume, MarginTradingHelpers.VolumeAccuracy);
        }

        public static void AddMatchedOrders(this IBaseOrder order, params MatchedOrder[] orders)
        {
            order.MatchedOrders = orders.Union(order.MatchedOrders ?? new MatchedOrder[0]).ToImmutableList();
        }
    }
}
