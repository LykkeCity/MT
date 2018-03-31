using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.AssetPairSettings
{
    [PublicAPI]
    public class AssetPairInputContract
    {
        /// <summary>
        /// Instrument display name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Base asset id
        /// </summary>
        public string BaseAssetId { get; set; }

        /// <summary>
        /// Quoting asset id
        /// </summary>
        public string QuoteAssetId { get; set; }

        /// <summary>
        /// Instrument accuracy in decimal digits count
        /// </summary>
        public int Accuracy { get; set; }

        /// <summary>
        /// Id of legal entity
        /// </summary>
        public string LegalEntity { get; set; }

        /// <summary>
        /// Base pair id (ex. BTCUSD for id BTCUSD.cy)
        /// </summary>
        public string BasePairId { get; set; }

        /// <summary>
        /// How should this asset pair be traded
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public MatchingEngineModeContract MatchingEngineMode { get; set; }

        /// <summary>
        /// Markup for bid for stp mode. 1 results in no changes.
        /// </summary>
        /// <remarks>
        /// You cannot specify a value lower or equal to 0 to ensure positive resulting values.
        /// </remarks>
        public decimal StpMultiplierMarkupBid { get; set; }

        /// <summary>
        /// Markup for ask for stp mode. 1 results in no changes.
        /// </summary>
        /// <remarks>
        /// You cannot specify a value lower or equal to 0 to ensure positive resulting values.
        /// </remarks>
        public decimal StpMultiplierMarkupAsk { get; set; }
    }
}