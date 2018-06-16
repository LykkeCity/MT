using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services
{
    public class OrderCacheGroup
    {
        private readonly Dictionary<string, Order> _ordersById;
        private readonly Dictionary<string, HashSet<string>> _orderIdsByAccountId;
        private readonly Dictionary<string, HashSet<string>> _orderIdsByInstrumentId;
        private readonly Dictionary<(string, string), HashSet<string>> _orderIdsByAccountIdAndInstrumentId;
        //private readonly Dictionary<string, HashSet<string>> _orderIdsByMarginInstrumentId;
        private readonly OrderStatus _status;
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public OrderCacheGroup(IReadOnlyCollection<Order> orders, OrderStatus status)
        {
            _status = status;

            var statusOrders = orders.Where(x => x.Status == status).ToList();

            _lockSlim.EnterWriteLock();

            try
            {
                _ordersById = statusOrders.ToDictionary(x => x.Id);

                _orderIdsByInstrumentId = statusOrders.GroupBy(x => x.AssetPairId)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());

                _orderIdsByAccountId = statusOrders.GroupBy(x => x.AccountId)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());

                _orderIdsByAccountIdAndInstrumentId = statusOrders.GroupBy(x => GetAccountInstrumentCacheKey(x.AccountId, x.AssetPairId))
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());

//                _orderIdsByMarginInstrumentId = statusOrders.Where(x => !string.IsNullOrEmpty(x.MarginCalcInstrument))
//                    .GroupBy(x => x.MarginCalcInstrument)
//                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());
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

                if (!_orderIdsByInstrumentId.ContainsKey(order.AssetPairId))
                    _orderIdsByInstrumentId.Add(order.AssetPairId, new HashSet<string>());
                _orderIdsByInstrumentId[order.AssetPairId].Add(order.Id);

                var accountInstrumentCacheKey = GetAccountInstrumentCacheKey(order.AccountId, order.AssetPairId);

                if (!_orderIdsByAccountIdAndInstrumentId.ContainsKey(accountInstrumentCacheKey))
                    _orderIdsByAccountIdAndInstrumentId.Add(accountInstrumentCacheKey, new HashSet<string>());
                _orderIdsByAccountIdAndInstrumentId[accountInstrumentCacheKey].Add(order.Id);

//                if (!string.IsNullOrEmpty(order.MarginCalcInstrument))
//                {
//                    if(!_orderIdsByMarginInstrumentId.ContainsKey(order.MarginCalcInstrument))
//                        _orderIdsByMarginInstrumentId.Add(order.MarginCalcInstrument, new HashSet<string>());
//                    _orderIdsByMarginInstrumentId[order.MarginCalcInstrument].Add(order.Id);
//                }
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            var account = MtServiceLocator.AccountsCacheService.Get(order.AccountId);
            account.CacheNeedsToBeUpdated();
        }

        public void Remove(Order order)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                if (_ordersById.Remove(order.Id))
                {
                    OnRemove(order);

//                    if (!string.IsNullOrEmpty(order.MarginCalcInstrument)
//                        && (_orderIdsByMarginInstrumentId[order.MarginCalcInstrument]?.Contains(order.Id) ?? false))
//                        _orderIdsByMarginInstrumentId[order.MarginCalcInstrument].Remove(order.Id);
                }
                else
                    throw new Exception(string.Format(MtMessages.CantRemoveOrderWithStatus, order.Id, _status));
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            var account = MtServiceLocator.AccountsCacheService?.Get(order.AccountId);
            account?.CacheNeedsToBeUpdated();
        }
        
        public bool TryPopById(string orderId, out Order result)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                if (!_ordersById.ContainsKey(orderId))
                {
                    result = null;
                    return false;
                }
                result = _ordersById[orderId];
                _ordersById.Remove(orderId);
                OnRemove(result);
                return true;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        private void OnRemove(Order order)
        {
            _orderIdsByInstrumentId[order.AssetPairId].Remove(order.Id);
            _orderIdsByAccountId[order.AccountId].Remove(order.Id);
            _orderIdsByAccountIdAndInstrumentId[GetAccountInstrumentCacheKey(order.AccountId, order.AssetPairId)]
                .Remove(order.Id);
        }

        #endregion


        #region Getters

        public Order GetOrderById(string orderId)
        {
            if (TryGetOrderById(orderId, out var result))
                return result;

            throw new Exception(string.Format(MtMessages.CantGetOrderWithStatus, orderId, _status));
        }

        public bool TryGetOrderById(string orderId, out Order result)
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
        
        public IReadOnlyCollection<Order> GetOrdersByInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            _lockSlim.EnterReadLock();

            try
            {
                if (!_orderIdsByInstrumentId.ContainsKey(instrument))
                    return new List<Order>();

                return _orderIdsByInstrumentId[instrument].Select(id => _ordersById[id]).ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

//        public IReadOnlyCollection<Order> GetOrdersByMarginInstrument(string instrument)
//        {
//            if (string.IsNullOrWhiteSpace(instrument))
//                throw new ArgumentException(nameof(instrument));
//
//            _lockSlim.EnterReadLock();
//
//            try
//            {
//                if (!_orderIdsByMarginInstrumentId.ContainsKey(instrument))
//                    return new List<Position>();
//
//                return _orderIdsByMarginInstrumentId[instrument].Select(id => _ordersById[id]).ToList();
//            }
//            finally
//            {
//                _lockSlim.ExitReadLock();
//            }
//        }

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
                if (!_orderIdsByAccountIdAndInstrumentId.ContainsKey(key))
                    return new List<Order>();

                return _orderIdsByAccountIdAndInstrumentId[key].Select(id => _ordersById[id]).ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public IReadOnlyCollection<Order> GetAllOrders()
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