using MarginTrading.MarketMaker.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.MarketMaker.Models.Api
{
    /// <summary>
    /// Message for controlling asset pair settings
    /// </summary>
    public class AssetPairSettingsModel
    {
        /// <summary>
        /// Asset pair id
        /// </summary>
        public string AssetPairId { get; set; }

        /// <summary>
        /// If this property is set - the quotes source for the asset pair will be changed to the passed value
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public AssetPairQuotesSourceTypeEnum? SetSourceType { get; set; }

        /// <summary>
        /// The price for sell order to create. Used only if the quotes source for the asset pair is manual
        /// </summary>
        public decimal? PriceForSellOrder { get; set; }

        /// <summary>
        /// The price for buy order to create. Used only if the quotes source for the asset pair is manual
        /// </summary>
        public decimal? PriceForBuyOrder { get; set; }
    }
}
