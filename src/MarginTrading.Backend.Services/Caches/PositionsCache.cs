using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services
{
    public class PositionsCache
    {
        private readonly Dictionary<string, Position> _ordersById;
        private readonly Dictionary<string, HashSet<string>> _orderIdsByAccountId;
        private readonly Dictionary<string, HashSet<string>> _orderIdsByInstrumentId;
        private readonly Dictionary<(string, string), HashSet<string>> _orderIdsByAccountIdAndInstrumentId;
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public PositionsCache(IReadOnlyCollection<Position> positions)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                _ordersById = positions.ToDictionary(x => x.Id);

                _orderIdsByInstrumentId = positions.GroupBy(x => x.Instrument)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());

                _orderIdsByAccountId = positions.GroupBy(x => x.AccountId)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());

                _orderIdsByAccountIdAndInstrumentId = positions.GroupBy(x => GetAccountInstrumentCacheKey(x.AccountId, x.Instrument))
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());
            }
            finally 
            {
                _lockSlim.ExitWriteLock();
            }
        }

        #region Setters

        public void Add(Position order)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                _ordersById.Add(order.Id, order);

                if (!_orderIdsByAccountId.ContainsKey(order.AccountId))
                    _orderIdsByAccountId.Add(order.AccountId, new HashSet<string>());
                _orderIdsByAccountId[order.AccountId].Add(order.Id);

                if (!_orderIdsByInstrumentId.ContainsKey(order.Instrument))
                    _orderIdsByInstrumentId.Add(order.Instrument, new HashSet<string>());
                _orderIdsByInstrumentId[order.Instrument].Add(order.Id);

                var accountInstrumentCacheKey = GetAccountInstrumentCacheKey(order.AccountId, order.Instrument);

                if (!_orderIdsByAccountIdAndInstrumentId.ContainsKey(accountInstrumentCacheKey))
                    _orderIdsByAccountIdAndInstrumentId.Add(accountInstrumentCacheKey, new HashSet<string>());
                _orderIdsByAccountIdAndInstrumentId[accountInstrumentCacheKey].Add(order.Id);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            var account = MtServiceLocator.AccountsCacheService.Get(order.AccountId);
            account.CacheNeedsToBeUpdated();
        }

        public void Remove(Position order)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                if (_ordersById.Remove(order.Id))
                {
                    _orderIdsByInstrumentId[order.Instrument].Remove(order.Id);
                    _orderIdsByAccountId[order.AccountId].Remove(order.Id);
                    _orderIdsByAccountIdAndInstrumentId[GetAccountInstrumentCacheKey(order.AccountId, order.Instrument)].Remove(order.Id);
                }
                else
                    throw new Exception(string.Format(MtMessages.CantRemovePosition, order.Id));
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            var account = MtServiceLocator.AccountsCacheService?.Get(order.AccountId);
            account?.CacheNeedsToBeUpdated();
        }

        #endregion


        #region Getters

        public Position GetOrderById(string orderId)
        {
            if (TryGetOrderById(orderId, out var result))
                return result;

            throw new Exception(string.Format(MtMessages.CantGetPosition, orderId));
        }

        public bool TryGetOrderById(string orderId, out Position result)
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

        public IReadOnlyCollection<Position> GetOrdersByInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            _lockSlim.EnterReadLock();

            try
            {
                if (!_orderIdsByInstrumentId.ContainsKey(instrument))
                    return new List<Position>();

                return _orderIdsByInstrumentId[instrument].Select(id => _ordersById[id]).ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public ICollection<Position> GetOrdersByInstrumentAndAccount(string instrument, string accountId)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException(nameof(instrument));

            var key = GetAccountInstrumentCacheKey(accountId, instrument);

            _lockSlim.EnterReadLock();

            try
            {
                if (!_orderIdsByAccountIdAndInstrumentId.ContainsKey(key))
                    return new List<Position>();

                return _orderIdsByAccountIdAndInstrumentId[key].Select(id => _ordersById[id]).ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public IReadOnlyCollection<Position> GetAllOrders()
        {
            _lockSlim.EnterReadLock();

            try
            {
                return _ordersById.Values.ToArray();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public ICollection<Position> GetOrdersByAccountIds(params string[] accountIds)
        {
            _lockSlim.EnterReadLock();

            var result = new List<Position>();

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