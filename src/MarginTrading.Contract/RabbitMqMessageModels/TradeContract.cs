// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.Contract.RabbitMqMessageModels
{
    public class TradeContract
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string OrderId { get; set; }
        public string AssetPairId { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public TradeType Type { get; set; }
        public DateTime Date { get; set; }
        public decimal Price { get; set; }
        public decimal Volume { get; set; }
    }

    public enum TradeType
    {
        Buy = 0,
        Sell = 1
    }
}