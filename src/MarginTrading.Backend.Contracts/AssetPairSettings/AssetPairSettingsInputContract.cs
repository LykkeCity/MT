using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Backend.Contracts.AssetPairSettings
{
    public class AssetPairSettingsInputContract
    {
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
        /// Markup for bid. 1 results in no changes.
        /// </summary>
        /// <remarks>
        /// You cannot specify a value lower or equal to 0 to ensure positive resulting values.
        /// </remarks>
        public decimal MultiplierMarkupBid { get; set; }
        
        /// <summary>
        /// Markup for ask. 1 results in no changes.
        /// </summary>
        /// <remarks>
        /// You cannot specify a value lower or equal to 0 to ensure positive resulting values.
        /// </remarks>
        public decimal MultiplierMarkupAsk { get; set; }
    }
}