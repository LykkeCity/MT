using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Services.Infrastructure;

namespace MarginTrading.Backend.Services
{
    public interface IOrderReader
    {
        ImmutableArray<Order> GetAll();
        ImmutableArray<Order> GetActive();
        ImmutableArray<Order> GetPending();
    }

    public class OrdersCache : IOrderReader
    {
        private readonly IContextFactory _contextFactory;

        public OrdersCache(IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            
            ActiveOrders = new OrderCacheGroup(new Order[0], OrderStatus.Active);
            WaitingForExecutionOrders = new OrderCacheGroup(new Order[0], OrderStatus.WaitingForExecution);
            ClosingOrders = new OrderCacheGroup(new Order[0], OrderStatus.Closing);
        }

        public OrderCacheGroup ActiveOrders { get; private set; }
        public OrderCacheGroup WaitingForExecutionOrders { get; private set; }
        public OrderCacheGroup ClosingOrders { get; private set; }

        public ImmutableArray<Order> GetAll()
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(OrdersCache)}.{nameof(GetAll)}"))
                return ActiveOrders.GetAllOrders()
                    .Union(WaitingForExecutionOrders.GetAllOrders())
                    .Union(ClosingOrders.GetAllOrders()).ToImmutableArray();
        }

        public ImmutableArray<Order> GetActive()
        {
            return ActiveOrders.GetAllOrders().ToImmutableArray();
        }

        public ImmutableArray<Order> GetPending()
        {
            return WaitingForExecutionOrders.GetAllOrders().ToImmutableArray();
        }

        public Order GetOrderById(string orderId)
        {
            if (WaitingForExecutionOrders.TryGetOrderById(orderId, out var result))
                return result;

            if (ActiveOrders.TryGetOrderById(orderId, out result))
                return result;

            throw new Exception(string.Format(MtMessages.OrderNotFound, orderId));
        }

        public void InitOrders(List<Order> orders)
        {
            ActiveOrders = new OrderCacheGroup(orders, OrderStatus.Active);
            WaitingForExecutionOrders = new OrderCacheGroup(orders, OrderStatus.WaitingForExecution);
            ClosingOrders = new OrderCacheGroup(orders, OrderStatus.Closing);
        }
    }
}