using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IOrderActionService
    {
        Task CreateOrderActionsForOrderHistory(Func<Task<IEnumerable<IOrderHistory>>> source, Func<IOrderAction, Task> destination);

        Task CreateOrderActionForClosedMarketOrder(IOrder order, Func<IOrderAction, Task> destination, bool realtime = true);

        Task CreateOrderActionForCancelledMarketOrder(IOrder order, Func<IOrderAction, Task> destination, bool realtime = true);

        Task CreateOrderActionForPlacedMarketOrder(IOrder order, Func<IOrderAction, Task> destination, bool realtime = true);
    }
}