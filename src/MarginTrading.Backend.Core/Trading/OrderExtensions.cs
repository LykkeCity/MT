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
        
        public static OrderDirection GetCloseType(this Position order)
        {
            return order.Volume >= 0 ? OrderDirection.Sell : OrderDirection.Buy;
        }

        public static decimal GetTotalFpl(this Position order, decimal swaps)
        {
            return order.GetFpl() - order.GetOpenCommission() - order.GetCloseCommission() - swaps;
        }

        public static decimal GetTotalFpl(this Position order)
        {
            return Math.Round(GetTotalFpl(order, order.GetSwaps()), order.CalculateFplData().AccountBaseAssetAccuracy);
        }

        private static FplData CalculateFplData(this Position order)
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

        public static decimal GetFpl(this Position order)
        {
            return order.CalculateFplData().Fpl;
        }

        public static decimal GetFplRate(this Position order)
        {
            return order.CalculateFplData().FplRate;
        }

        public static decimal GetMarginRate(this Position order)
        {
            return order.CalculateFplData().MarginRate;
        }

        public static decimal GetMarginMaintenance(this Position order)
        {
            return order.CalculateFplData().MarginMaintenance;
        }

        public static decimal GetMarginInit(this Position order)
        {
            return order.CalculateFplData().MarginInit;
        }

        public static void UpdatePendingOrderMargin(this Position order)
        {
            if (order is Position orderInstance)
            {
                orderInstance.FplData.ActualHash++;
            }
        }

        public static decimal GetSwaps(this Position order)
        {
            return MtServiceLocator.SwapCommissionService.GetSwaps(order);
        }

        public static decimal GetOpenCommission(this Position order)
        {
            return Math.Abs(order.Volume) * order.OpenCommission;
        }

        public static decimal GetCloseCommission(this Position order)
        {
            return Math.Abs(order.Volume) * order.CloseCommission;
        }

        public static OrderDirection GetOrderDirectionToMatchInOrderBook(this OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
        }
    }
}