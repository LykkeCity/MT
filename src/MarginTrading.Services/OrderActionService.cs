using MarginTrading.AzureRepositories;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Services
{
    public class OrderActionService : IOrderActionService
    {
        public async Task CreateOrderActionForCancelledMarketOrder(IOrder order, Func<IOrderAction, Task> destination, bool realtime = true)
        {
            var orderAction = new OrderAction();

            orderAction.CoreSide = order.GetOrderType() == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
            orderAction.CoreSymbol = order.Instrument;
            orderAction.ExecutionDuration = CalculateExecutionDurationForClose(order);
            orderAction.LykkeOrderId = order.Id;
            orderAction.TraderAction = TraderAction.Close;
            orderAction.TimeForceCondition = TimeForceCondition.None;
            orderAction.TraderExternalSymbol = order.Instrument;
            orderAction.TraderLykkeId = order.ClientId;
            orderAction.IsLive = realtime;
            orderAction.OrderId = $"{order.Id}_{TraderAction.Close}";

            await destination(orderAction);
        }

        public async Task CreateOrderActionForClosedMarketOrder(IOrder order, Func<IOrderAction, Task> destination, bool realtime = true)
        {
            var orderAction = new OrderAction();

            orderAction.CoreSide = order.GetOrderType() == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
            orderAction.CoreSymbol = order.Instrument;
            orderAction.ExecutionDuration = CalculateExecutionDurationForClose(order);
            orderAction.LykkeOrderId = order.Id;
            orderAction.TraderAction = TraderAction.Close;
            orderAction.TimeForceCondition = GetTimeForceCongition(order.CloseReason);
            orderAction.TraderExternalSymbol = order.Instrument;
            orderAction.TraderLykkeId = order.ClientId;
            orderAction.IsLive = realtime;
            orderAction.OrderId = $"{order.Id}_{TraderAction.Close}";

            await destination(orderAction);
        }

        public async Task CreateOrderActionForPlacedMarketOrder(IOrder order, Func<IOrderAction, Task> destination, bool realtime = true)
        {
            var orderAction = new OrderAction();

            orderAction.CoreSide = order.GetOrderType();
            orderAction.CoreSymbol = order.Instrument;
            orderAction.ExecutionDuration = CalculateExecutionDurationForOpen(order);
            orderAction.LykkeOrderId = order.Id;
            orderAction.TraderAction = TraderAction.Open;
            orderAction.TimeForceCondition = TimeForceCondition.None;
            orderAction.TraderExternalSymbol = order.Instrument;
            orderAction.TraderLykkeId = order.ClientId;
            orderAction.IsLive = realtime;
            orderAction.OrderId = $"{order.Id}_{TraderAction.Open}";

            await destination(orderAction);
        }

        public async Task CreateOrderActionsForOrderHistory(Func<Task<IEnumerable<IOrderHistory>>> source, Func<IOrderAction, Task> destination)
        {
            foreach (IOrderHistory historyOrder in await source())
            {
                var order = MarginTradingOrderHistoryEntity.Restore(historyOrder);

                await CreateOrderActionForPlacedMarketOrder(order, destination, false);

                await CreateOrderActionForClosedMarketOrder(order, destination, false);
            }
        }

        private double? CalculateExecutionDurationForClose(IOrder order)
        {
            if (order.CloseDate.HasValue && order.StartClosingDate.HasValue)
                return Math.Abs((order.CloseDate.Value - order.StartClosingDate.Value).Milliseconds);
            return null;
        }

        private TimeForceCondition GetTimeForceCongition(OrderCloseReason closeReason)
        {
            switch (closeReason)
            {
                case OrderCloseReason.StopLoss:
                    return TimeForceCondition.StopLoss;
                case OrderCloseReason.StopOut:
                    return TimeForceCondition.StopOut;
                case OrderCloseReason.TakeProfit:
                    return TimeForceCondition.TakeProfit;
                default:
                    return TimeForceCondition.None;
            }
        }

        private double? CalculateExecutionDurationForOpen(IOrder order)
        {
            if (order.OpenDate.HasValue)
                return Math.Abs((order.OpenDate.Value - order.CreateDate).Milliseconds);
            return null;
        }
    }
}