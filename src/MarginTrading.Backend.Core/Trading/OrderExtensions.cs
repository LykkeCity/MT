using System;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public static class OrderExtensions
    {
        public static bool IsSuitablePriceForPendingOrder(this Order order, decimal price)
        {
            switch (order.OrderType)
            {
                case OrderType.Limit:
                case OrderType.TakeProfit:
                    return order.Direction == OrderDirection.Buy && price <= order.Price
                           || order.Direction == OrderDirection.Sell && price >= order.Price;
                case OrderType.Stop:
                case OrderType.StopLoss:
                    return order.Direction == OrderDirection.Buy && price >= order.Price
                           || order.Direction == OrderDirection.Sell && price <= order.Price;
                default:
                    return false;
            }
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
            return Math.Abs(order.Volume) * order.OpenCommissionRate;
        }

        public static decimal GetCloseCommission(this Position order)
        {
            return Math.Abs(order.Volume) * order.CloseCommissionRate;
        }

        public static OrderDirection GetOpositeDirection(this OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
        }
        
        public static PositionDirection GetClosePositionDirection(this OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? PositionDirection.Short : PositionDirection.Long;
        }
        
        public static OrderDirection GetOrderDirectionToMatchInOrderBook(this OrderDirection orderType)
        {
            return orderType.GetOpositeDirection();
        }

        public static bool IsBasicPending(this Order order)
        {
            return order.OrderType == OrderType.Limit || order.OrderType == OrderType.Stop;
        }

        public static PositionCloseReason GetCloseReason(this OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.StopLoss:
                    return PositionCloseReason.StopLoss;
                case OrderType.TakeProfit:
                    return PositionCloseReason.TakeProfit;
                default:
                    return PositionCloseReason.Close;
            }
        }
    }
}