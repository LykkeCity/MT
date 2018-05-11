using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Common.Json;
using StackExchange.Redis;
using Order = MarginTrading.Backend.Core.Order;

namespace MarginTrading.Backend.Services
{
    public class OrderCacheGroup : IOrderCacheGroup
    {
        private const string RedisPartitionKey = ":Orders:";
        private string RedisRouteKey { get; set; }

        private string OrdersByIdKey { get; set; }
        private string OrderIdsByAccountIdKey { get; set; }
        private string OrderIdsByInstrumentIdKey { get; set; }
        private string OrderIdsByAccountIdAndInstrumentIdKey { get; set; }
        private string OrderIdsByMarginInstrumentIdKey { get; set; }
        
        private IDatabase Database { get; set; }
        
        public OrderStatus Status { get; }
        
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public OrderCacheGroup(IDatabase database, OrderStatus status)
        {
            Database = database;
            Status = status;
            
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
                await Database.HashSetAsync(OrdersByIdKey, order.Id, order.Serialize());

                await Database.SetAddAsync($"{OrderIdsByInstrumentIdKey}:{order.Instrument}", order.Id);

                await Database.SetAddAsync($"{OrderIdsByAccountIdKey}:{order.AccountId}", order.Id);

                await Database.SetAddAsync(
                    $"{OrderIdsByAccountIdAndInstrumentIdKey}:{GetAccountInstrumentCacheKey(order.AccountId, order.Instrument)}",
                    order.Id);

                if (!string.IsNullOrEmpty(order.MarginCalcInstrument))
                {
                    await Database.SetAddAsync($"{OrderIdsByMarginInstrumentIdKey}:{order.MarginCalcInstrument}", order.Id);
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
                    await Database.SetRemoveAsync($"{OrderIdsByInstrumentIdKey}:{order.Instrument}", order.Id);
                    await Database.SetRemoveAsync($"{OrderIdsByAccountIdKey}:{order.AccountId}", order.Id);
                    await Database.SetRemoveAsync(
                        $"{OrderIdsByAccountIdAndInstrumentIdKey}:{GetAccountInstrumentCacheKey(order.AccountId, order.Instrument)}",
                        order.Id);

                    if (!string.IsNullOrEmpty(order.MarginCalcInstrument))
                    {
                        await Database.SetRemoveAsync($"{OrderIdsByMarginInstrumentIdKey}:{order.MarginCalcInstrument}", order.Id);
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

        public Order GetOrderById(string orderId)
        {
            if (TryGetOrderById(orderId, out var result))
                return result;

            throw new Exception(string.Format(MtMessages.CantGetOrderWithStatus, orderId, Status));
        }

        public bool TryGetOrderById(string orderId, out Order result)
        {
            _semaphoreSlim.Wait();

            try
            {
                //TODO must be substituted with async version
                var data = Database.HashGet(OrdersByIdKey, orderId);
                return data.TryDeserialize(out result);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public IReadOnlyCollection<Order> GetOrdersByInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            _semaphoreSlim.Wait();

            try
            {
                //TODO optimize
                var orderIdsData = Database.SetMembers($"{OrderIdsByInstrumentIdKey}:{instrument}");
                var orderIds = orderIdsData.Select(x => CacheSerializer.Deserialize<string>(x)).ToList();
                var orders = orderIds.Select(x =>
                {
                    var data = Database.HashGet(OrdersByIdKey, x);
                    data.TryDeserialize(out var result);
                    return result;
                }).ToList();
                return orders;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public IReadOnlyCollection<Order> GetOrdersByMarginInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            _semaphoreSlim.Wait();

            try
            {
                //TODO optimize
                var orderIdsData = Database.SetMembers($"{OrderIdsByMarginInstrumentIdKey}:{instrument}");
                var orderIds = orderIdsData.Select(x => CacheSerializer.Deserialize<string>(x)).ToList();
                var orders = orderIds.Select(x =>
                {
                    var data = Database.HashGet(OrdersByIdKey, x);
                    data.TryDeserialize(out var result);
                    return result;
                }).ToList();
                return orders;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public ICollection<Order> GetOrdersByInstrumentAndAccount(string instrument, string accountId)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException(nameof(instrument));

            var key = GetAccountInstrumentCacheKey(accountId, instrument);

            _semaphoreSlim.Wait();

            try
            {
                //TODO optimize
                var orderIdsData = Database.SetMembers(
                    $"{OrderIdsByAccountIdAndInstrumentIdKey}:{GetAccountInstrumentCacheKey(accountId, instrument)}");
                var orderIds = orderIdsData.Select(x => CacheSerializer.Deserialize<string>(x)).ToList();
                var orders = orderIds.Select(x =>
                {
                    var data = Database.HashGet(OrdersByIdKey, x);
                    data.TryDeserialize(out var result);
                    return result;
                }).ToList();
                return orders;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public IReadOnlyCollection<Order> GetAllOrders()
        {
            _semaphoreSlim.Wait();

            try
            {
                //TODO optimize
                var ordersData = Database.HashGetAll(OrdersByIdKey);
                var orders = ordersData.Select(x => x.Deserialize()).ToList();
                return orders;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public ICollection<Order> GetOrdersByAccountIds(params string[] accountIds)
        {
            _semaphoreSlim.Wait();

            try
            {
                var result = new List<Order>();
                foreach (var accountId in accountIds)
                {
                    //TODO optimize
                    //todo maybe it's faster to grab $"{OrderIdsByAccountIdKey}:" and sort locally... depends on cases
                    var orderIdsData = Database.SetMembers($"{OrderIdsByAccountIdKey}:{accountId}");
                    var orderIds = orderIdsData.Select(x => CacheSerializer.Deserialize<string>(x)).ToList();
                    var orders = orderIds.Select(x =>
                    {
                        var data = Database.HashGet(OrdersByIdKey, x);
                        data.TryDeserialize(out var deserializedOrder);
                        return deserializedOrder;
                    }).ToList();
                    
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