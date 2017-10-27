using System;
using MarginTrading.MarketMaker.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.MarketMaker.Models.Api
{
    public class AssetPairSettings: IAssetPairSettingsEntity
    {
        public string AssetPairId { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public AssetPairQuotesSourceTypeEnum QuotesSourceType { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}