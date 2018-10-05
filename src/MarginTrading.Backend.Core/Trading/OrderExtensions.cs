using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
                case OrderType.TrailingStop:
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

        public static decimal GetTotalFpl(this Position position, decimal swaps)
        {
            return position.GetFpl() - position.GetOpenCommission() - position.GetCloseCommission() - swaps -
                   position.ChargedPnL;
        }

        public static decimal GetTotalFpl(this Position order)
        {
            return Math.Round(GetTotalFpl(order, order.GetSwaps()), order.CalculateFplData().AccountBaseAssetAccuracy);
        }

        private static FplData CalculateFplData(this Position position)
        {
            if (position.FplData.ActualHash != position.FplData.CalculatedHash || position.FplData.ActualHash == 0)
            {
                MtServiceLocator.FplService.UpdatePositionFpl(position);
            }

            return position.FplData;
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
        
        public static bool IsBasicPendingOrder(this Order order)
        {
            return order.OrderType == OrderType.Limit || order.OrderType == OrderType.Stop;
        }

        public static bool IsBasicOrder(this Order order)
        {
            return order.OrderType == OrderType.Market ||
                   order.IsBasicPendingOrder();
        }
        
        public static PositionCloseReason GetCloseReason(this OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.StopLoss:
                case OrderType.TrailingStop:
                    return PositionCloseReason.StopLoss;
                case OrderType.TakeProfit:
                    return PositionCloseReason.TakeProfit;
                default:
                    return PositionCloseReason.Close;
            }
        }

        public static byte GetExecutionRank(this OrderType orderType)
        {
            switch (orderType)
            {
                case OrderType.Market:
                    return 0;
                case OrderType.Stop:
                case OrderType.StopLoss:
                case OrderType.TrailingStop:
                    return 2;
                case OrderType.Limit:
                case OrderType.TakeProfit:
                    return 4;
                default:
                    return 8;
            }
        }

        public static byte GetExecutionRank(this OrderDirection orderDirection)
        {
            switch (orderDirection)
            {
                case OrderDirection.Sell:
                    return 0;
                case OrderDirection.Buy:
                    return 1;
                default:
                    return 0;
            }
        }
        
        public static ICollection<Order> GetSortedForExecution(this IEnumerable<Order> orders)
        {
            return orders.OrderBy(o => o.ExecutionRank)
                .ThenBy(o => o.ExecutionPriceRank)
                .ThenBy(o => o.Created)
                .ToList();
        }
    }
}