using System;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Core
{
    public interface IAssetPair
    {
        /// <summary>
        /// Instrument id
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Instrument display name
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Base asset id
        /// </summary>
        string BaseAssetId { get; }
        
        /// <summary>
        /// Quoting asset id
        /// </summary>
        string QuoteAssetId { get; }
        
        /// <summary>
        /// Instrument accuracy in decimal digits count
        /// </summary>
        int Accuracy { get; }

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
        /// Markup for bid for stp mode. 1 results in no changes.
        /// </summary>
        /// <remarks>
        /// You cannot specify a value lower or equal to 0 to ensure positive resulting values.
        /// </remarks>
        decimal StpMultiplierMarkupBid { get; }

        /// <summary>
        /// Markup for ask for stp mode. 1 results in no changes.
        /// </summary>
        /// <remarks>
        /// You cannot specify a value lower or equal to 0 to ensure positive resulting values.
        /// </remarks>
        decimal StpMultiplierMarkupAsk { get; }
    }

    public class AssetPair : IAssetPair
    {
        public AssetPair(string id, string name, string baseAssetId,
            string quoteAssetId, int accuracy, string legalEntity,
            string basePairId, MatchingEngineMode matchingEngineMode, decimal stpMultiplierMarkupBid,
            decimal stpMultiplierMarkupAsk)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            BaseAssetId = baseAssetId ?? throw new ArgumentNullException(nameof(baseAssetId));
            QuoteAssetId = quoteAssetId ?? throw new ArgumentNullException(nameof(quoteAssetId));
            Accuracy = accuracy;
            
            LegalEntity = legalEntity.RequiredNotNullOrWhiteSpace(nameof(legalEntity));
            BasePairId = basePairId.RequiredNotNullOrWhiteSpace(nameof(basePairId));
            MatchingEngineMode = matchingEngineMode.RequiredEnum(nameof(matchingEngineMode));
            StpMultiplierMarkupBid = stpMultiplierMarkupBid.RequiredGreaterThan(0, nameof(stpMultiplierMarkupBid));
            StpMultiplierMarkupAsk = stpMultiplierMarkupAsk.RequiredGreaterThan(0, nameof(stpMultiplierMarkupAsk));
        }

        public string Id { get; }
        public string Name { get; }
        public string BaseAssetId { get; }
        public string QuoteAssetId { get; }
        public int Accuracy { get; }
        
        public string LegalEntity { get; }
        public string BasePairId { get; }
        public MatchingEngineMode MatchingEngineMode { get; }
        public decimal StpMultiplierMarkupBid { get; }
        public decimal StpMultiplierMarkupAsk { get; }
    }
}
