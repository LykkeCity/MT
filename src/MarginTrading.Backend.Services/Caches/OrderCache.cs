using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Infrastructure;

namespace MarginTrading.Backend.Services
{
    public interface IOrderReader
    {
        ImmutableArray<Order> GetAllOrders();
        ImmutableArray<Position> GetPositions();
        ImmutableArray<Order> GetPending();
    }

    public class OrdersCache : IOrderReader
    {
        private readonly IContextFactory _contextFactory;

        public OrdersCache(IContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            
            Active = new OrderCacheGroup(new Order[0], OrderStatus.Active);
            Inactive = new OrderCacheGroup(new Order[0], OrderStatus.Inactive);
            InProgress = new OrderCacheGroup(new Order[0], OrderStatus.ExecutionStarted);
            Positions = new PositionsCache(new Position[0]);
        }

        public OrderCacheGroup Active { get; private set; }
        public OrderCacheGroup Inactive { get; private set; }
        public OrderCacheGroup InProgress { get; private set; }
        public PositionsCache Positions { get; private set; }
        
        public ImmutableArray<Order> GetAllOrders()
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(OrdersCache)}.{nameof(GetAllOrders)}"))
                return Active.GetAllOrders()
                    .Union(Inactive.GetAllOrders())
                    .Union(InProgress.GetAllOrders()).ToImmutableArray();
        }

        public ImmutableArray<Position> GetPositions()
        {
            return Positions.GetAllOrders().ToImmutableArray();
        }

        public ImmutableArray<Order> GetPending()
        {
            return Active.GetAllOrders().ToImmutableArray();
        }

//        public ImmutableArray<Position> GetPendingForMarginRecalc(string instrument)
//        {
//            return WaitingForExecutionOrders.GetOrdersByMarginInstrument(instrument).ToImmutableArray();
//        }

        public bool TryGetOrderById(string orderId, out Order order)
        {
            return Active.TryGetOrderById(orderId, out order) ||
                   Inactive.TryGetOrderById(orderId, out order) || 
                    InProgress.TryGetOrderById(orderId, out order);
        }
        
        public Order GetOrderById(string orderId)
        {
            if (TryGetOrderById(orderId, out var result))
                return result;

            throw new Exception(string.Format(MtMessages.OrderNotFound, orderId));
        }

        public void InitOrders(List<Order> orders, List<Position> positions)
        {
            Active = new OrderCacheGroup(orders, OrderStatus.Active);
            Inactive = new OrderCacheGroup(orders, OrderStatus.Inactive);
            InProgress = new OrderCacheGroup(orders, OrderStatus.ExecutionStarted);
            Positions = new PositionsCache(positions);
        }
    }
}