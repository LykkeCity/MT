using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Services.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Order = MarginTrading.Backend.Core.Order;

namespace MarginTrading.Backend.Services
{
    public interface IOrderReader
    {
        Task<ImmutableArray<Order>> GetAll();
        Task<ImmutableArray<Order>> GetActive();
        Task<ImmutableArray<Order>> GetPending();
    }

    public class OrdersCache : IOrderReader
    {
        private readonly IContextFactory _contextFactory;

        public OrdersCache(IContextFactory contextFactory,
            IOrderCacheGroup[] orderCacheGroups)
        {
            _contextFactory = contextFactory;

            ActiveOrders = orderCacheGroups.First(x => x.Status == OrderStatus.Active).Init(new Order[0]);
            WaitingForExecutionOrders = orderCacheGroups.First(x => x.Status == OrderStatus.WaitingForExecution).Init(new Order[0]);
            ClosingOrders = orderCacheGroups.First(x => x.Status == OrderStatus.Closing).Init(new Order[0]);
        }

        public IOrderCacheGroup ActiveOrders { get; private set; }
        public IOrderCacheGroup WaitingForExecutionOrders { get; private set; }
        public IOrderCacheGroup ClosingOrders { get; private set; }
        
        public async Task<ImmutableArray<Order>> GetAll()
        {
            return (await ActiveOrders.GetAllOrders())
                    .Union(await WaitingForExecutionOrders.GetAllOrders())
                    .Union(await ClosingOrders.GetAllOrders()).ToImmutableArray();
        }

        public async Task<ImmutableArray<Order>> GetActive()
        {
            return (await ActiveOrders.GetAllOrders()).ToImmutableArray();
        }

        public async Task<ImmutableArray<Order>> GetPending()
        {
            return (await WaitingForExecutionOrders.GetAllOrders()).ToImmutableArray();
        }

        public async Task<ImmutableArray<Order>> GetPendingForMarginRecalc(string instrument)
        {
            return (await WaitingForExecutionOrders.GetOrdersByMarginInstrument(instrument)).ToImmutableArray();
        }

        public async Task<Order> GetOrderByIdOrDefault(string orderId)
        {
            var order = await WaitingForExecutionOrders.GetOrderByIdOrDefault(orderId);
            if (order != null)
            {
                return order;
            }
            order = await ActiveOrders.GetOrderByIdOrDefault(orderId);
            return order;
        }
        
        public async Task<Order> GetOrderById(string orderId)
        {
            var order = await GetOrderByIdOrDefault(orderId);
            if (order != null)
                return order;

            throw new Exception(string.Format(MtMessages.OrderNotFound, orderId));
        }

        public void InitOrders(List<Order> orders)
        {
            ActiveOrders = ActiveOrders.Init(orders);
            WaitingForExecutionOrders = WaitingForExecutionOrders.Init(orders);
            ClosingOrders = ClosingOrders.Init(orders);
        }
    }
}