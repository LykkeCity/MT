using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using MarginTrading.Core;
using MarginTrading.Core.Messages;

namespace MarginTrading.Services
{
    public class OrderCacheGroup
    {
        private readonly Dictionary<string, Order> _ordersById;
        private readonly Dictionary<string, HashSet<string>> _orderIdsByAccountId;
        private readonly Dictionary<string, HashSet<string>> _ordersIdsByInstrumentId;
        private readonly Dictionary<(string, string), HashSet<string>> _ordersIdsByAccountIdAndInstrumentId;
        private readonly OrderStatus _status;
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public OrderCacheGroup(IEnumerable<Order> orders, OrderStatus status)
        {
            _status = status;

            var statusOrders = orders.Where(x => x.Status == status).ToList();

            _lockSlim.EnterWriteLock();

            try
            {
                _ordersById = statusOrders.ToDictionary(x => x.Id);

                _ordersIdsByInstrumentId = statusOrders.GroupBy(x => x.Instrument)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());

                _orderIdsByAccountId = statusOrders.GroupBy(x => x.AccountId)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());

                _ordersIdsByAccountIdAndInstrumentId = statusOrders.GroupBy(x => GetAccountInstrumentCacheKey(x.AccountId, x.Instrument))
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());
            }
            finally 
            {
                _lockSlim.ExitWriteLock();
            }
        }


        #region Setters

        public void Add(Order order)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                _ordersById.Add(order.Id, order);

                if (!_orderIdsByAccountId.ContainsKey(order.AccountId))
                    _orderIdsByAccountId.Add(order.AccountId, new HashSet<string>());
                _orderIdsByAccountId[order.AccountId].Add(order.Id);

                if (!_ordersIdsByInstrumentId.ContainsKey(order.Instrument))
                    _ordersIdsByInstrumentId.Add(order.Instrument, new HashSet<string>());
                _ordersIdsByInstrumentId[order.Instrument].Add(order.Id);

                var accountInstrumentCacheKey = GetAccountInstrumentCacheKey(order.AccountId, order.Instrument);

                if (!_ordersIdsByAccountIdAndInstrumentId.ContainsKey(accountInstrumentCacheKey))
                    _ordersIdsByAccountIdAndInstrumentId.Add(accountInstrumentCacheKey, new HashSet<string>());
                _ordersIdsByAccountIdAndInstrumentId[accountInstrumentCacheKey].Add(order.Id);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            var account = MtServiceLocator.AccountsCacheService.Get(order.ClientId, order.AccountId);
            account.CacheNeedsToBeUpdated();
        }

        public void Remove(Order order)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                if (_ordersById.Remove(order.Id))
                {
                    _ordersIdsByInstrumentId[order.Instrument].Remove(order.Id);
                    _orderIdsByAccountId[order.AccountId].Remove(order.Id);
                    _ordersIdsByAccountIdAndInstrumentId[
                        GetAccountInstrumentCacheKey(order.AccountId, order.Instrument)].Remove(order.Id);
                }
                else
                    throw new Exception(string.Format(MtMessages.CantRemoveOrderWithStatus, order.Id, _status));
            }
            finally
            {
                _lockSlim.ExitWriteLock();
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

            throw new Exception(string.Format(MtMessages.CantGetOrderWithStatus, orderId, _status));
        }

        internal bool TryGetOrderById(string orderId, out Order result)
        {
            _lockSlim.EnterReadLock();

            try
            {
                if (!_ordersById.ContainsKey(orderId))
                {
                    result = null;
                    return false;
                }
                result = _ordersById[orderId];
                return true;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public ICollection<Order> GetOrdersByInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            _lockSlim.EnterReadLock();

            try
            {
                if (!_ordersIdsByInstrumentId.ContainsKey(instrument))
                    return new List<Order>();

                return _ordersIdsByInstrumentId[instrument].Select(id => _ordersById[id]).ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public ICollection<Order> GetOrdersByInstrumentAndAccount(string instrument, string accountId)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException(nameof(instrument));

            var key = GetAccountInstrumentCacheKey(accountId, instrument);

            _lockSlim.EnterReadLock();

            try
            {
                if (!_ordersIdsByAccountIdAndInstrumentId.ContainsKey(key))
                    return new List<Order>();

                return _ordersIdsByAccountIdAndInstrumentId[key].Select(id => _ordersById[id]).ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public ICollection<Order> GetAllOrders()
        {
            _lockSlim.EnterReadLock();

            try
            {
                return _ordersById.Values;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public ICollection<Order> GetOrdersByAccountIds(params string[] accountIds)
        {
            _lockSlim.EnterReadLock();

            var result = new List<Order>();

            try
            {
                foreach (var accountId in accountIds)
                {
                    if (!_orderIdsByAccountId.ContainsKey(accountId))
                        continue;

                    foreach (var orderId in _orderIdsByAccountId[accountId])
                        result.Add(_ordersById[orderId]);
                }

                return result;
            }
            finally
            {
                _lockSlim.ExitReadLock();
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