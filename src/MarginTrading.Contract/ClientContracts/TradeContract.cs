using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Contract.ClientContracts
{
    public class TradeClientContract
    {
        public string Id { get; set; }
        public string OrderId { get; set; }
        public string AssetPairId { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TradeClientType Type { get; set; }
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
    }

    public enum TradeClientType
    {
        Buy = 0,
        Sell = 1
    }
}