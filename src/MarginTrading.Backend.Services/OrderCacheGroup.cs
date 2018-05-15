using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Json;
using StackExchange.Redis;
using Order = MarginTrading.Backend.Core.Order;

namespace MarginTrading.Backend.Services
{
    public class OrderCacheGroup : IOrderCacheGroup
    {
        private string RedisPartitionKey { get; set; }
        private string RedisRouteKey { get; set; }

        private string OrdersByIdKey { get; set; }
        private string OrderIdsByAccountIdKey { get; set; }
        private string OrderIdsByInstrumentIdKey { get; set; }
        private string OrderIdsByAccountIdAndInstrumentIdKey { get; set; }
        private string OrderIdsByMarginInstrumentIdKey { get; set; }
        
        private IDatabase Database { get; set; }
        
        public OrderStatus Status { get; }
        
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public OrderCacheGroup(IDatabase database, OrderStatus status, MarginSettings marginSettings)
        {
            Database = database;
            Status = status;

            var env = marginSettings.IsLive ? "Live" : "Demo";
            RedisPartitionKey = $":Orders{env}:";
            RedisRouteKey = SelectRedisKey(status);
            OrdersByIdKey = GetCacheKey("OrdersById:");
            OrderIdsByAccountIdKey = GetCacheKey("OrderIdsByAccountId:");
            OrderIdsByInstrumentIdKey = GetCacheKey("OrderIdsByInstrumentId:");
            OrderIdsByAccountIdAndInstrumentIdKey = GetCacheKey("OrderIdsByAccountIdAndInstrumentId:");
            OrderIdsByMarginInstrumentIdKey = GetCacheKey("OrderIdsByMarginInstrumentId:");
        }

        public IOrderCacheGroup Init(IReadOnlyCollection<Order> orders)
        {
            var statusOrders = orders.Where(x => x.Status == Status).ToList();

            InitAsync(statusOrders).GetAwaiter().GetResult();

            return this;
        }

        private string GetCacheKey(string id)
        {
            return $"{RedisPartitionKey}{RedisRouteKey}{id}";
        }

        private static string SelectRedisKey(OrderStatus status)
        {
            switch (status)
            {
                case OrderStatus.WaitingForExecution:
                    return "Pending:";
                case OrderStatus.Active:
                    return "Open:";
                case OrderStatus.Closing:
                    return "Closing:";
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
            return null;
        }

        private async Task InitAsync(IReadOnlyCollection<Order> statusOrders)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                await Database.HashSetAsync(OrdersByIdKey, statusOrders.Serialize(x => x.Id));

                //TODO may be optimized to query data concurrently + throttled by SemaphoreSlim
                var orderIdsByInstrumentId = statusOrders.GroupBy(x => x.Instrument)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());
                foreach (var kvp in orderIdsByInstrumentId)
                {
                    await Database.SetAddAsync($"{OrderIdsByInstrumentIdKey}:{kvp.Key}",
                        kvp.Value.Serialize());
                }

                var orderIdsByAccountId = statusOrders.GroupBy(x => x.AccountId)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());
                foreach (var kvp in orderIdsByAccountId)
                {
                    await Database.SetAddAsync($"{OrderIdsByAccountIdKey}:{kvp.Key}",
                        kvp.Value.Serialize());
                }

                var orderIdsByAccountIdAndInstrumentId = statusOrders
                    .GroupBy(x => GetAccountInstrumentCacheKey(x.AccountId, x.Instrument))
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());
                foreach (var kvp in orderIdsByAccountIdAndInstrumentId)
                {
                    await Database.SetAddAsync($"{OrderIdsByAccountIdAndInstrumentIdKey}:{kvp.Key}",
                        kvp.Value.Serialize());
                }

                var orderIdsByMarginInstrumentId = statusOrders.Where(x => !string.IsNullOrEmpty(x.MarginCalcInstrument))
                    .GroupBy(x => x.MarginCalcInstrument)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());
                foreach (var kvp in orderIdsByMarginInstrumentId)
                {
                    await Database.SetAddAsync($"{OrderIdsByMarginInstrumentIdKey}:{kvp.Key}",
                        kvp.Value.Serialize());
                }
            }
            finally 
            {
                _semaphoreSlim.Release();
            } 
        }

        #region Setters

        public async Task AddAsync(Order order)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                var orderIdSerialized = CacheSerializer.Serialize(order.Id);
                
                await Database.HashSetAsync(OrdersByIdKey, order.Id, order.Serialize());
                await Database.SetAddAsync($"{OrderIdsByInstrumentIdKey}:{order.Instrument}", orderIdSerialized);
                await Database.SetAddAsync($"{OrderIdsByAccountIdKey}:{order.AccountId}", orderIdSerialized);
                await Database.SetAddAsync(
                    $"{OrderIdsByAccountIdAndInstrumentIdKey}:{GetAccountInstrumentCacheKey(order.AccountId, order.Instrument)}",
                    orderIdSerialized);

                if (!string.IsNullOrEmpty(order.MarginCalcInstrument))
                {
                    await Database.SetAddAsync($"{OrderIdsByMarginInstrumentIdKey}:{order.MarginCalcInstrument}", orderIdSerialized);
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            var account = MtServiceLocator.AccountsCacheService.Get(order.ClientId, order.AccountId);
            account.CacheNeedsToBeUpdated();
        }

        public async Task RemoveAsync(Order order)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                if (await Database.HashDeleteAsync(OrdersByIdKey, order.Id))
                {
                    var orderIdSerialized = CacheSerializer.Serialize(order.Id);
                        
                    await Database.SetRemoveAsync($"{OrderIdsByInstrumentIdKey}:{order.Instrument}", orderIdSerialized);
                    await Database.SetRemoveAsync($"{OrderIdsByAccountIdKey}:{order.AccountId}", orderIdSerialized);
                    await Database.SetRemoveAsync(
                        $"{OrderIdsByAccountIdAndInstrumentIdKey}:{GetAccountInstrumentCacheKey(order.AccountId, order.Instrument)}",
                        orderIdSerialized);

                    if (!string.IsNullOrEmpty(order.MarginCalcInstrument))
                    {
                        await Database.SetRemoveAsync($"{OrderIdsByMarginInstrumentIdKey}:{order.MarginCalcInstrument}", orderIdSerialized);
                    }
                }
                else
                    throw new Exception(string.Format(MtMessages.CantRemoveOrderWithStatus, order.Id, Status));
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            var account = MtServiceLocator.AccountsCacheService?.Get(order.ClientId, order.AccountId);
            account?.CacheNeedsToBeUpdated();
        }

        #endregion


        #region Getters

        public async Task<Order> GetOrderById(string orderId)
        {
            var order = await GetOrderByIdOrDefault(orderId);
            if (order != null)
                return order;

            throw new Exception(string.Format(MtMessages.CantGetOrderWithStatus, orderId, Status));
        }

        public async Task<Order> GetOrderByIdOrDefault(string orderId)
        {
            _semaphoreSlim.Wait();

            try
            {
                var data = await Database.HashGetAsync(OrdersByIdKey, orderId);
                return data.TryDeserialize(out var result) ? result : null;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<IReadOnlyCollection<Order>> GetOrdersByInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            _semaphoreSlim.Wait();

            try
            {
                //TODO optimize
                var orderIdsData = await Database.SetMembersAsync($"{OrderIdsByInstrumentIdKey}:{instrument}");
                var orderIds = orderIdsData.Select(x => x.Deserialize<string>()).ToList();
                var orders = new List<Order>();
                foreach (var orderId in orderIds)
                {
                    var data = await Database.HashGetAsync(OrdersByIdKey, orderId);
                    data.TryDeserialize(out var result);
                    orders.Add(result);
                }
                return orders;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<IReadOnlyCollection<Order>> GetOrdersByMarginInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            _semaphoreSlim.Wait();

            try
            {
                //TODO optimize
                var orderIdsData = await Database.SetMembersAsync($"{OrderIdsByMarginInstrumentIdKey}:{instrument}");
                var orderIds = orderIdsData.Select(x => x.Deserialize<string>()).ToList();
                var orders = new List<Order>();
                foreach (var orderId in orderIds)
                {
                    var data = await Database.HashGetAsync(OrdersByIdKey, orderId);
                    data.TryDeserialize(out var result);
                    orders.Add(result);
                }
                return orders;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<ICollection<Order>> GetOrdersByInstrumentAndAccount(string instrument, string accountId)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException(nameof(instrument));

            _semaphoreSlim.Wait();

            try
            {
                //TODO optimize
                var orderIdsData = await Database.SetMembersAsync(
                    $"{OrderIdsByAccountIdAndInstrumentIdKey}:{GetAccountInstrumentCacheKey(accountId, instrument)}");
                var orderIds = orderIdsData.Select(x => x.Deserialize<string>()).ToList();
                var orders = new List<Order>();
                foreach (var orderId in orderIds)
                {
                    var data = await Database.HashGetAsync(OrdersByIdKey, orderId);
                    data.TryDeserialize(out var result);
                    orders.Add(result);
                }
                return orders;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<IReadOnlyCollection<Order>> GetAllOrders()
        {
            _semaphoreSlim.Wait();

            try
            {
                var ordersData = await Database.HashGetAllAsync(OrdersByIdKey);
                var orders = ordersData.Select(x => x.Deserialize<Order>()).ToList();
                return orders;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<ICollection<Order>> GetOrdersByAccountIds(params string[] accountIds)
        {
            _semaphoreSlim.Wait();

            try
            {
                var result = new List<Order>();
                foreach (var accountId in accountIds)
                {
                    //TODO optimize
                    //todo maybe it's faster to grab $"{OrderIdsByAccountIdKey}:" and sort locally... depends on cases
                    var orderIdsData = await Database.SetMembersAsync($"{OrderIdsByAccountIdKey}:{accountId}");
                    var orderIds = orderIdsData.Select(x => x.Deserialize<string>()).ToList();
                    var orders = new List<Order>();
                    foreach (var orderId in orderIds)
                    {
                        var data = await Database.HashGetAsync(OrdersByIdKey, orderId);
                        data.TryDeserialize(out var order);
                        orders.Add(order);
                    }
                    
                    result.AddRange(orders);
                }

                return result;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
        #endregion


        #region Helpers

        private (string, string) GetAccountInstrumentCacheKey(string accountId, string instrumentId)
        {
            return (accountId, instrumentId);
        }

        #endregion
    }
}