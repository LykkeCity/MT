using System;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public static class OrderExtensions
    {
        public static bool IsSuitablePriceForPendingOrder(this Order order, decimal price)
        {
            return order.Price.HasValue && (order.Direction == OrderDirection.Buy && price <= order.Price
                                            || order.Direction == OrderDirection.Sell && price >= order.Price);
        }

        public static bool IsStopLoss(this IPosition order)
        {
            return order.GetOrderDirection() == OrderDirection.Buy
                ? order.StopLoss.HasValue && order.StopLoss.Value > 0 && order.ClosePrice <= order.StopLoss
                : order.StopLoss.HasValue && order.StopLoss.Value > 0 && order.ClosePrice >= order.StopLoss;
        }

        public static bool IsTakeProfit(this IPosition order)
        {
            return order.GetOrderDirection() == OrderDirection.Buy
                ? order.TakeProfit.HasValue && order.TakeProfit > 0 && order.ClosePrice >= order.TakeProfit
                : order.TakeProfit.HasValue && order.TakeProfit > 0 && order.ClosePrice <= order.TakeProfit;
        }

        public static decimal GetTotalFpl(this IPosition order, decimal swaps)
        {
            return order.GetFpl() - order.GetOpenCommission() - order.GetCloseCommission() - swaps;
        }

        public static decimal GetTotalFpl(this IPosition order)
        {
            return Math.Round(GetTotalFpl(order, order.GetSwaps()), order.CalculateFplData().AccountBaseAssetAccuracy);
        }

        public static decimal GetMatchedVolume(this IPosition order)
        {
            return order.MatchedOrders.SummaryVolume;
        }

        public static decimal GetMatchedCloseVolume(this IPosition order)
        {
            return order.MatchedCloseOrders.SummaryVolume;
        }

        public static decimal GetRemainingCloseVolume(this IPosition order)
        {
            return order.GetMatchedVolume() - order.GetMatchedCloseVolume();
        }

        public static bool GetIsCloseFullfilled(this IPosition order)
        {
            return Math.Round(order.GetRemainingCloseVolume(), MarginTradingHelpers.VolumeAccuracy) == 0;
        }

        private static FplData CalculateFplData(this IPosition order)
        {
            if (order is Position orderInstance)
            {
                if (orderInstance.FplData.ActualHash != orderInstance.FplData.CalculatedHash)
                {
                    MtServiceLocator.FplService.UpdateOrderFpl(orderInstance, orderInstance.FplData);
                }

                return orderInstance.FplData;
            }

            var fplData = new FplData();
            MtServiceLocator.FplService.UpdateOrderFpl(order, fplData);

            return fplData;
        }

        public static decimal GetFpl(this IPosition order)
        {
            return order.CalculateFplData().Fpl;
        }

        public static decimal GetFplRate(this IPosition order)
        {
            return order.CalculateFplData().FplRate;
        }

        public static decimal GetMarginRate(this IPosition order)
        {
            return order.CalculateFplData().MarginRate;
        }

        public static decimal GetMarginMaintenance(this IPosition order)
        {
            return order.CalculateFplData().MarginMaintenance;
        }

        public static decimal GetMarginInit(this IPosition order)
        {
            return order.CalculateFplData().MarginInit;
        }

        public static void UpdateClosePrice(this IPosition order, decimal closePrice)
        {
            if (order is Position orderInstance)
            {
                orderInstance.ClosePrice = closePrice;
                orderInstance.FplData.ActualHash++;
                var account = MtServiceLocator.AccountsCacheService.Get(order.AccountId);
                account.CacheNeedsToBeUpdated();
            }
        }

        public static void UpdatePendingOrderMargin(this IPosition order)
        {
            if (order is Position orderInstance)
            {
                orderInstance.FplData.ActualHash++;
            }
        }

        public static decimal GetSwaps(this IPosition order)
        {
            return MtServiceLocator.SwapCommissionService.GetSwaps(order);
        }

        public static decimal GetOpenCommission(this IPosition order)
        {
            return Math.Abs(order.Volume) * order.OpenCommission;
        }

        public static decimal GetCloseCommission(this IPosition order)
        {
            return Math.Abs(order.GetMatchedCloseVolume()) * order.CloseCommission;
        }

        public static OrderDirection GetOrderDirectionToMatchInOrderBook(this OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
        }
    }
}