// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Extensions;

namespace MarginTrading.Backend.Services
{
    /// <inheritdoc />
    /// Current implementation is not thread safe
    public class DraftSnapshotKeeper : IDraftSnapshotKeeper
    {
        private DateTime? _tradingDay;
        
        [CanBeNull]
        private TradingEngineSnapshot _snapshot;
        
        [CanBeNull]
        private List<Position> _positions;
        
        [CanBeNull]
        private List<Order> _orders;
        
        [CanBeNull]
        private List<MarginTradingAccount> _accounts;

        [CanBeNull] 
        private List<BestPriceContract> _fxPrices;

        [CanBeNull] 
        private List<BestPriceContract> _cfdQuotes;
        
        private readonly ITradingEngineSnapshotsRepository _tradingEngineSnapshotsRepository;
        
        public DraftSnapshotKeeper(ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository)
        {
            _tradingEngineSnapshotsRepository = tradingEngineSnapshotsRepository;
        }
        
        #region IDraftSnapshotKeeper implementation

        /// <inheritdoc />
        public DateTime TradingDay
        {
            get
            {
                if (!_tradingDay.HasValue)
                    throw new InvalidOperationException("The draft snapshot provider has not been initialized yet");

                return _tradingDay.Value;
            }
        }

        /// <inheritdoc />
        public List<BestPriceContract> FxPrices
        {
            get
            {
                if (_fxPrices != null)
                    return _fxPrices;
                
                EnsureSnapshotLoadedOrThrowAsync().GetAwaiter().GetResult();

                _fxPrices = _snapshot.GetBestFxPrices().Values.ToList();

                return _fxPrices;
            }
        }

        /// <inheritdoc />
        public List<BestPriceContract> CfdQuotes
        {
            get
            {
                if (_cfdQuotes != null)
                    return _cfdQuotes;
                
                EnsureSnapshotLoadedOrThrowAsync().GetAwaiter().GetResult();

                _cfdQuotes = _snapshot.GetBestTradingPrices().Values.ToList();
                
                return _cfdQuotes;
            }
        }

        /// <inheritdoc />
        public IDraftSnapshotKeeper Init(DateTime tradingDay)
        {
            if (_tradingDay.HasValue && _tradingDay != tradingDay)
                throw new InvalidOperationException(
                    $"The draft snapshot provider has already been initialized with trading day [{_tradingDay}]");

            _tradingDay = tradingDay;

            return this;
        }

        /// <inheritdoc />
        public async ValueTask<bool> ExistsAsync()
        {
            if (_snapshot != null)
                return true;
            
            if (!_tradingDay.HasValue)
                throw new InvalidOperationException("The draft snapshot provider has not been initialized yet");

            return await _tradingEngineSnapshotsRepository.DraftExistsAsync(_tradingDay.Value);
        }

        /// <inheritdoc />
        public async ValueTask<List<MarginTradingAccount>> GetAccountsAsync()
        {
            if (_accounts != null)
                return _accounts;

            await EnsureSnapshotLoadedOrThrowAsync();

            _accounts = _snapshot.GetAccountsFromDraft();

            return _accounts;
        }
        
        /// <inheritdoc />
        public async Task UpdateAsync(ImmutableArray<Position> positions, 
            ImmutableArray<Order> orders, 
            ImmutableArray<MarginTradingAccount> accounts,
            IEnumerable<BestPriceContract> fxRates,
            IEnumerable<BestPriceContract> cfdQuotes)
        {
            if (!_tradingDay.HasValue)
                throw new InvalidOperationException("Unable to update snapshot: the draft snapshot provider has not been initialized yet");

            if (positions == null || !positions.Any())
                throw new ArgumentNullException(nameof(positions), @"Unable to update snapshot: positions list is empty");

            if (orders == null || !orders.Any())
                throw new ArgumentNullException(nameof(orders), @"Unable to update snapshot: orders list is empty");

            if (accounts == null || !accounts.Any())
                throw new ArgumentNullException(nameof(accounts), @"Unable to update snapshot: accounts list is empty");

            await EnsureSnapshotLoadedOrThrowAsync();
            
            var fxPrices = fxRates?.ToDictionary(r => r.Id, r => r);

            var tradingPrices = cfdQuotes?.ToDictionary(q => q.Id, q => q);

            _snapshot = new TradingEngineSnapshot(_snapshot.TradingDay,
                _snapshot.CorrelationId,
                _snapshot.Timestamp,
                orders.ToJson(),
                positions.ToJson(),
                accounts.ToJson(),
                _snapshot
                    .GetBestFxPrices()
                    .Merge(fxPrices)
                    .ToJson(),
                _snapshot
                    .GetBestTradingPrices()
                    .Merge(tradingPrices)
                    .ToJson(),
                _snapshot.Status);

            // to force keeper deserialize updated values from json next time data is accessed
            _positions = null;
            _orders = null;
            _accounts = null;
            _fxPrices = null;
            _cfdQuotes = null;
        }

        #endregion
        
        #region IOrderReader implementation
        public ImmutableArray<Order> GetAllOrders()
        {
            if (_orders != null)
                return _orders.ToImmutableArray();
            
            if (!_tradingDay.HasValue)
                throw new InvalidOperationException("Unable to provide orders: the draft snapshot provider has not been initialized yet");

            EnsureSnapshotLoadedOrThrowAsync().GetAwaiter().GetResult();

            _orders = _snapshot.GetOrdersFromDraft();

            return _orders.ToImmutableArray();
        }

        public ImmutableArray<Position> GetPositions()
        {
            if (_positions != null)
                return _positions.ToImmutableArray();
            
            if (!_tradingDay.HasValue)
                throw new InvalidOperationException("Unable to provide positions: the draft snapshot provider has not been initialized yet");

            EnsureSnapshotLoadedOrThrowAsync().GetAwaiter().GetResult();

            _positions = _snapshot.GetPositionsFromDraft();

            return _positions.ToImmutableArray();
        }

        public ImmutableArray<Position> GetPositions(string instrument)
        {
            if (string.IsNullOrEmpty(instrument))
                throw new ArgumentNullException(nameof(instrument));

            return GetPositions()
                .Where(p => p.AssetPairId == instrument)
                .ToImmutableArray();
        }

        public ImmutableArray<Position> GetPositionsByFxAssetPairId(string fxAssetPairId)
        {
            if (string.IsNullOrEmpty(fxAssetPairId))
                throw new ArgumentNullException(nameof(fxAssetPairId));

            return GetPositions()
                .Where(p => p.FxAssetPairId == fxAssetPairId)
                .ToImmutableArray();
        }

        public ImmutableArray<Order> GetPending() =>
            GetAllOrders()
                .Where(o => o.Status == OrderStatus.Active)
                .ToImmutableArray();

        public bool TryGetOrderById(string orderId, out Order order)
        {
            order = GetAllOrders().SingleOrDefault(o => o.Id == orderId);
            return order != null;
        }
        
        #endregion
        
        private async Task EnsureSnapshotLoadedOrThrowAsync()
        {
            if (_snapshot == null)
            {
                if (!_tradingDay.HasValue)
                    throw new InvalidOperationException("The draft snapshot provider has not been initialized yet");

                _snapshot = await _tradingEngineSnapshotsRepository.GetLastDraftAsync(_tradingDay) ??
                            throw new TradingSnapshotDraftNotFoundException(_tradingDay.Value);
            }
        }
    }
}