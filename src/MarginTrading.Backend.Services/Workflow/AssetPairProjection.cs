using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Common.Extensions;
using MarginTrading.SettingsService.Contracts.AssetPair;

namespace MarginTrading.Backend.Services.Workflow
{
    /// <summary>
    /// Listens to <see cref="AssetPairChangedEvent"/>s and builds a projection inside of the
    /// <see cref="IAssetPairsCache"/>
    /// </summary>
    [UsedImplicitly]
    public class AssetPairProjection
    {
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IScheduleSettingsCacheService _scheduleSettingsCacheService;
        private readonly ILog _log;

        public AssetPairProjection(
            IAssetPairsCache assetPairsCache,
            IScheduleSettingsCacheService scheduleSettingsCacheService,
            ILog log)
        {
            _assetPairsCache = assetPairsCache;
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
                _assetPairsCache.Remove(@event.AssetPair.Id);
            }
            else
            {
                _assetPairsCache.AddOrUpdate(new AssetPair(
                    id: @event.AssetPair.Id,
                    name: @event.AssetPair.Name,
                    baseAssetId: @event.AssetPair.BaseAssetId,
                    quoteAssetId: @event.AssetPair.QuoteAssetId,
                    accuracy: @event.AssetPair.Accuracy,
                    legalEntity: @event.AssetPair.LegalEntity,
                    basePairId: @event.AssetPair.BasePairId,
                    matchingEngineMode: @event.AssetPair.MatchingEngineMode.ToType<MatchingEngineMode>(),
                    stpMultiplierMarkupBid: @event.AssetPair.StpMultiplierMarkupBid,
                    stpMultiplierMarkupAsk: @event.AssetPair.StpMultiplierMarkupAsk,
                    isSuspended: @event.AssetPair.IsSuspended,
                    isFrozen: @event.AssetPair.IsFrozen,
                    isDiscontinued: @event.AssetPair.IsDiscontinued
                ));
            }

            await _scheduleSettingsCacheService.UpdateSettingsAsync();
        }

        private static bool IsDelete(AssetPairChangedEvent @event)
        {
            return @event.AssetPair.BaseAssetId == null || @event.AssetPair.QuoteAssetId == null;
        }
    }
}