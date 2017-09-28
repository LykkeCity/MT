using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;
using MarginTrading.Core.Messages;
using MarginTrading.Services.Infrastructure;

namespace MarginTrading.Services
{
    public class OrderCacheGroup
    {
        private readonly Dictionary<string, Order> _ordersById;
        private readonly Dictionary<string, Dictionary<string, Order>> _ordersByAccountId;
        private readonly Dictionary<string, Dictionary<string, Order>> _ordersByInstrumentId;
        private readonly OrderStatus _status;
        private readonly IContextFactory _contextFactory;

        public OrderCacheGroup(IEnumerable<Order> orders, OrderStatus status, IContextFactory contextFactory)
        {
            _status = status;
            _contextFactory = contextFactory;

            var statusOrders = orders.Where(x => x.Status == status).ToList();

            _ordersById = statusOrders.ToDictionary(x => x.Id);

            _ordersByInstrumentId = statusOrders.GroupBy(x => x.Instrument)
                .ToDictionary(x => x.Key, x => x.ToDictionary(y => y.Id));

            _ordersByAccountId = statusOrders.GroupBy(x => x.AccountId)
                .ToDictionary(x => x.Key, x => x.ToDictionary(y => y.Id));
        }

        #region Setters
        public void Add(Order order)
        {
            if (!_ordersByAccountId.ContainsKey(order.AccountId))
                _ordersByAccountId.Add(order.AccountId, new Dictionary<string, Order>());
            _ordersByAccountId[order.AccountId].Add(order.Id, order);

            if (!_ordersByInstrumentId.ContainsKey(order.Instrument))
                _ordersByInstrumentId.Add(order.Instrument, new Dictionary<string, Order>());
            _ordersByInstrumentId[order.Instrument].Add(order.Id, order);

            _ordersById.Add(order.Id, order);

            var account = MtServiceLocator.AccountsCacheService.Get(order.ClientId, order.AccountId);
            account.CacheNeedsToBeUpdated();
        }

        public void Remove(Order order)
        {
            if (_ordersById.Remove(order.Id))
            {
                _ordersByInstrumentId[order.Instrument].Remove(order.Id);
                _ordersByAccountId[order.AccountId].Remove(order.Id);

                var account = MtServiceLocator.AccountsCacheService.Get(order.ClientId, order.AccountId);
                account.CacheNeedsToBeUpdated();
            }
            else
                throw new Exception(string.Format(MtMessages.CantRemoveOrderWithStatus, order.Id, _status));
        }
        #endregion

        #region Getters
        public Order GetOrderById(string orderId)
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(OrderCacheGroup)}.{nameof(GetOrderById)}"))
            {
                Order result;
                if (TryGetOrderById(orderId, out result))
                    return result;

                throw new Exception(string.Format(MtMessages.CantGetOrderWithStatus, orderId, _status));
            }
        }

        internal bool TryGetOrderById(string orderId, out Order result)
        {
            if (!_ordersById.ContainsKey(orderId))
            {
                result = null;
                return false;
            }
            result = _ordersById[orderId];
            return true;
        }

        public IEnumerable<Order> GetOrders(string instrument)
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(OrderCacheGroup)}.{nameof(GetOrders)}_ByInstrument"))
            {
                if (string.IsNullOrWhiteSpace(instrument))
                    throw new ArgumentException(nameof(instrument));

                if (!_ordersByInstrumentId.ContainsKey(instrument))
                    return Array.Empty<Order>();

                var result = _ordersByInstrumentId[instrument].Values;

                // TODO: Cache modified ToArray once
                return result.ToArray();
            }
        }

        // TODO: Optimize it somehow
        public IEnumerable<Order> GetOrders(string instrument, string accountId)
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(OrderCacheGroup)}.{nameof(GetOrders)}_ByInstrumentAndAccount"))
            {
                return GetOrders(instrument).Where(x => x.AccountId == accountId);
            }
        }

        public IEnumerable<Order> GetAllOrders()
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(OrderCacheGroup)}.{nameof(GetAllOrders)}"))
            {
                return _ordersByAccountId.Values.SelectMany(accountId => accountId.Values);
            }
        }

        // TODO: Optimize
        public IEnumerable<Order> GetOrdersByAccountIds(params string[] accountIds)
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(OrderCacheGroup)}.{nameof(GetAllOrders)}_ByAccounts"))
            {
                foreach (var accountId in accountIds)
                {
                    if (!_ordersByAccountId.ContainsKey(accountId))
                        continue;
                    foreach (var order in _ordersByAccountId[accountId].Values)
                        yield return order;
                }
            }
        }
        #endregion
    }
}