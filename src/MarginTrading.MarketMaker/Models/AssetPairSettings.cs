using System;
using System.Collections.Generic;
using System.Text;
using MarginTrading.MarketMaker.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.MarketMaker.Models
{
    internal interface IAssetPairSettingsEntity
    {
        string AssetName { get; set; }

        /// <summary>
        /// Quotes source type
        /// </summary>
        AssetPairQuotesSourceTypeEnum QuotesSourceType { get; set; }

        /// <summary>
        /// External exchange which will be used for getting quotes, if <see cref="QuotesSourceType"/>
        /// is set to <see cref="AssetPairQuotesSourceTypeEnum.External"/>
        /// </summary>
        string ExternalExchange { get; set; }

        DateTimeOffset Timestamp { get; set; }
    }

    public class AssetPairSettings: IAssetPairSettingsEntity
    {
        public string AssetName { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public AssetPairQuotesSourceTypeEnum QuotesSourceType { get; set; }
        public string ExternalExchange { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}
