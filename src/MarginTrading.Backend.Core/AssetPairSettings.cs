using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Core
{
    public interface IAssetPairSettings
    {
        /// <summary>
        /// Instrument id
        /// </summary>
        string AssetPairId { get; }

        /// <summary>
        /// Id of legal entity
        /// </summary>
        string LegalEntity { get; }

        /// <summary>
        /// Base pair id (ex. BTCUSD for id BTCUSD.cy)
        /// </summary>
        string BasePairId { get; }

        /// <summary>
        /// How should this asset pair be traded
        /// </summary>
        MatchingEngineMode MatchingEngineMode { get; }

        /// <summary>
        /// Markup for bid. 1 results in no changes.
        /// </summary>
        /// <remarks>
        /// You cannot specify a value lower or equal to 0 to ensure positive resulting values.
        /// </remarks>
        decimal MultiplierMarkupBid { get; }

        /// <summary>
        /// Markup for ask. 1 results in no changes.
        /// </summary>
        /// <remarks>
        /// You cannot specify a value lower or equal to 0 to ensure positive resulting values.
        /// </remarks>
        decimal MultiplierMarkupAsk { get; }
    }

    public class AssetPairSettings : IAssetPairSettings
    {
        public string AssetPairId { get; }
        public string LegalEntity { get; }
        public string BasePairId { get; }
        public MatchingEngineMode MatchingEngineMode { get; }
        public decimal MultiplierMarkupBid { get; }
        public decimal MultiplierMarkupAsk { get; }

        public AssetPairSettings([NotNull] string assetPairId, [NotNull] string legalEntity,
            [NotNull] string basePairId, MatchingEngineMode matchingEngineMode, decimal multiplierMarkupBid,
            decimal multiplierMarkupAsk)
        {
            AssetPairId = assetPairId.RequiredNotNullOrWhiteSpace(nameof(assetPairId));
            LegalEntity = legalEntity.RequiredNotNullOrWhiteSpace(nameof(legalEntity));
            BasePairId = basePairId.RequiredNotNullOrWhiteSpace(nameof(basePairId));
            MatchingEngineMode = matchingEngineMode.RequiredEnum(nameof(matchingEngineMode));
            MultiplierMarkupBid = multiplierMarkupBid.RequiredGreaterThan(0, nameof(multiplierMarkupBid));
            MultiplierMarkupAsk = multiplierMarkupAsk.RequiredGreaterThan(0, nameof(multiplierMarkupAsk));
            
            //todo: Check AssetPairId suffix? Ex: .vu means LegalEntity should be LYKKEVU
            //todo: Check AssetPairId without suffix equals BasePairId?
        }
    }
}