using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
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

        public AssetPairProjection(
            IAssetPairsCache assetPairsCache)
        {
            _assetPairsCache = assetPairsCache;
        }

        [UsedImplicitly]
        public void Handle(AssetPairChangedEvent @event)
        {
            //idempotency handling is not required, it's ok if an object is updated multiple times
            
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
    }
}