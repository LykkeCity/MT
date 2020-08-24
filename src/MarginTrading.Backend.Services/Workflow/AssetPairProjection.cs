// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Extensions;
using MarginTrading.AssetService.Contracts.AssetPair;

namespace MarginTrading.Backend.Services.Workflow
{
    /// <summary>
    /// Listens to <see cref="AssetPairChangedEvent"/>s and builds a projection inside of the
    /// <see cref="IAssetPairsCache"/>
    /// </summary>
    [UsedImplicitly]
    public class AssetPairProjection
    {
        private readonly ITradingEngine _tradingEngine;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IOrderReader _orderReader;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
        private readonly ILog _log;

        public AssetPairProjection(
            ITradingEngine tradingEngine,
            IAssetPairsCache assetPairsCache,
            IOrderReader orderReader,
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            ILog log)
        {
            _tradingEngine = tradingEngine;
            _assetPairsCache = assetPairsCache;
            _orderReader = orderReader;
            _scheduleSettingsCacheService = scheduleSettingsCacheService;
            _log = log;
        }

        [UsedImplicitly]
        public async Task Handle(AssetPairChangedEvent @event)
        {
            //deduplication is not required, it's ok if an object is updated multiple times
            if (@event.AssetPair?.Id == null)
            {
                await _log.WriteWarningAsync(nameof(AssetPairProjection), nameof(Handle),
                    "AssetPairChangedEvent contained no asset pair id");
                return;
            }

            if (IsDelete(@event))
            {
                CloseAllOrders();

                ValidatePositions(@event.AssetPair.Id);
                
                _assetPairsCache.Remove(@event.AssetPair.Id);
            }
            else
            {
                if (@event.AssetPair.IsDiscontinued)
                {
                    CloseAllOrders();
                }
                
                var isAdded = _assetPairsCache.AddOrUpdate(new AssetPair(
                    id: @event.AssetPair.Id,
                    name: @event.AssetPair.Name,
                    baseAssetId: @event.AssetPair.BaseAssetId,
                    quoteAssetId: @event.AssetPair.QuoteAssetId,
                    accuracy: @event.AssetPair.Accuracy,
                    marketId: @event.AssetPair.MarketId,
                    legalEntity: @event.AssetPair.LegalEntity,
                    basePairId: @event.AssetPair.BasePairId,
                    matchingEngineMode: @event.AssetPair.MatchingEngineMode.ToType<MatchingEngineMode>(),
                    stpMultiplierMarkupBid: @event.AssetPair.StpMultiplierMarkupBid,
                    stpMultiplierMarkupAsk: @event.AssetPair.StpMultiplierMarkupAsk,
                    isSuspended: @event.AssetPair.IsSuspended,
                    isFrozen: @event.AssetPair.IsFrozen,
                    isDiscontinued: @event.AssetPair.IsDiscontinued
                ));
                
                if (isAdded)
                    await _scheduleSettingsCacheService.UpdateScheduleSettingsAsync();
            }

            void CloseAllOrders()
            {
                try
                {
                    foreach (var order in _orderReader.GetPending().Where(x => x.AssetPairId == @event.AssetPair.Id))
                    {
                        _tradingEngine.CancelPendingOrder(order.Id, null,@event.OperationId, 
                            null, OrderCancellationReason.InstrumentInvalidated);
                    }
                }
                catch (Exception exception)
                {
                    _log.WriteError(nameof(AssetPairProjection), nameof(CloseAllOrders), exception);
                    throw;
                }
            }
            
            void ValidatePositions(string assetPairId)
            {
                var positions = _orderReader.GetPositions(assetPairId);
                if (positions.Any())
                {
                    _log.WriteFatalError(nameof(AssetPairProjection), nameof(ValidatePositions), 
                        new Exception($"{positions.Length} positions are opened for [{assetPairId}], first: [{positions.First().Id}]."));
                }
            }
        }

        private static bool IsDelete(AssetPairChangedEvent @event)
        {
            return @event.AssetPair.BaseAssetId == null || @event.AssetPair.QuoteAssetId == null;
        }
    }
}