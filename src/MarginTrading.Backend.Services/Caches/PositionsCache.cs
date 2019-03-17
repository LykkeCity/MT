using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Services
{
    public class PositionsCache
    {
        private readonly Dictionary<string, Position> _positionsById;
        private readonly Dictionary<string, HashSet<string>> _positionIdsByAccountId;
        private readonly Dictionary<string, HashSet<string>> _positionIdsByInstrumentId;
        private readonly Dictionary<string, HashSet<string>> _positionIdsByFxInstrumentId;
        private readonly Dictionary<(string, string), HashSet<string>> _positionIdsByAccountIdAndInstrumentId;
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public PositionsCache(IReadOnlyCollection<Position> positions)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                _positionsById = positions.ToDictionary(x => x.Id);

                _positionIdsByInstrumentId = positions.GroupBy(x => x.AssetPairId)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());
                
                _positionIdsByFxInstrumentId = positions.GroupBy(x => x.FxAssetPairId)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());

                _positionIdsByAccountId = positions.GroupBy(x => x.AccountId)
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());

                _positionIdsByAccountIdAndInstrumentId = positions.GroupBy(x => GetAccountInstrumentCacheKey(x.AccountId, x.AssetPairId))
                    .ToDictionary(x => x.Key, x => x.Select(o => o.Id).ToHashSet());
            }
            finally 
            {
                _lockSlim.ExitWriteLock();
            }
        }

        #region Setters

        public void Add(Position position)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                _positionsById.Add(position.Id, position);

                if (!_positionIdsByAccountId.ContainsKey(position.AccountId))
                    _positionIdsByAccountId.Add(position.AccountId, new HashSet<string>());
                _positionIdsByAccountId[position.AccountId].Add(position.Id);

                if (!_positionIdsByInstrumentId.ContainsKey(position.AssetPairId))
                    _positionIdsByInstrumentId.Add(position.AssetPairId, new HashSet<string>());
                _positionIdsByInstrumentId[position.AssetPairId].Add(position.Id);

                if (!_positionIdsByFxInstrumentId.ContainsKey(position.FxAssetPairId))
                    _positionIdsByFxInstrumentId.Add(position.FxAssetPairId, new HashSet<string>());
                _positionIdsByFxInstrumentId[position.FxAssetPairId].Add(position.Id);

                var accountInstrumentCacheKey = GetAccountInstrumentCacheKey(position.AccountId, position.AssetPairId);

                if (!_positionIdsByAccountIdAndInstrumentId.ContainsKey(accountInstrumentCacheKey))
                    _positionIdsByAccountIdAndInstrumentId.Add(accountInstrumentCacheKey, new HashSet<string>());
                _positionIdsByAccountIdAndInstrumentId[accountInstrumentCacheKey].Add(position.Id);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            var account = MtServiceLocator.AccountsCacheService.Get(position.AccountId);
            account.CacheNeedsToBeUpdated();
        }

        public void Remove(Position order)
        {
            _lockSlim.EnterWriteLock();

            try
            {
                if (_positionsById.Remove(order.Id))
                {
                    _positionIdsByInstrumentId[order.AssetPairId].Remove(order.Id);
                    _positionIdsByFxInstrumentId[order.FxAssetPairId].Remove(order.Id);
                    _positionIdsByAccountId[order.AccountId].Remove(order.Id);
                    _positionIdsByAccountIdAndInstrumentId[GetAccountInstrumentCacheKey(order.AccountId, order.AssetPairId)].Remove(order.Id);
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

        public Position GetPositionById(string positionId)
        {
            if (TryGetPositionById(positionId, out var result))
                return result;

            throw new PositionNotFoundException(string.Format(MtMessages.CantGetPosition, positionId));
        }
        
        public List<Position> GetPositionsByIds(List<string> positionIds)
        {
            var result = new List<Position>();
            
            _lockSlim.EnterReadLock();
            
            try
            {
                foreach (var positionId in positionIds)
                {
                    if (TryGetPositionById(positionId, out var position))
                        result.Add(position);
                }
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }

            if (result.Count == 0)
            {
                throw new PositionNotFoundException($"Cannot get positions with ids [{string.Join(",", positionIds)}]");
            }

            return result;
        }

        public bool TryGetPositionById(string positionId, out Position result)
        {
            _lockSlim.EnterReadLock();

            try
            {
                return TryGetPositionByIdUnsafe(positionId, out result);
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        private bool TryGetPositionByIdUnsafe(string orderId, out Position result)
        {
            if (!_positionsById.ContainsKey(orderId))
            {
                result = null;
                return false;
            }
            result = _positionsById[orderId];
            return true;
        }

        public IReadOnlyCollection<Position> GetPositionsByInstrument(string instrument)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            _lockSlim.EnterReadLock();

            try
            {
                return _positionIdsByInstrumentId.ContainsKey(instrument)
                    ? _positionIdsByInstrumentId[instrument].Select(id => _positionsById[id]).ToList()
                    : new List<Position>();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public IReadOnlyCollection<Position> GetPositionsByFxInstrument(string fxInstrument)
        {
            if (string.IsNullOrWhiteSpace(fxInstrument))
                throw new ArgumentException(nameof(fxInstrument));

            _lockSlim.EnterReadLock();

            try
            {
                return _positionIdsByFxInstrumentId.ContainsKey(fxInstrument)
                    ? _positionIdsByFxInstrumentId[fxInstrument].Select(id => _positionsById[id]).ToList()
                    : new List<Position>();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public ICollection<Position> GetPositionsByInstrumentAndAccount(string instrument, string accountId)
        {
            if (string.IsNullOrWhiteSpace(instrument))
                throw new ArgumentException(nameof(instrument));

            if (string.IsNullOrWhiteSpace(accountId))
                throw new ArgumentException(nameof(instrument));

            var key = GetAccountInstrumentCacheKey(accountId, instrument);

            _lockSlim.EnterReadLock();

            try
            {
                if (!_positionIdsByAccountIdAndInstrumentId.ContainsKey(key))
                    return new List<Position>();

                return _positionIdsByAccountIdAndInstrumentId[key].Select(id => _positionsById[id]).ToList();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public IReadOnlyCollection<Position> GetAllPositions()
        {
            _lockSlim.EnterReadLock();

            try
            {
                return _positionsById.Values.ToArray();
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public ICollection<Position> GetPositionsByAccountIds(params string[] accountIds)
        {
            _lockSlim.EnterReadLock();

            var result = new List<Position>();

            try
            {
                foreach (var accountId in accountIds)
                {
                    if (!_positionIdsByAccountId.ContainsKey(accountId))
                        continue;

                    foreach (var orderId in _positionIdsByAccountId[accountId])
                        result.Add(_positionsById[orderId]);
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