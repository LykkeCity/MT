using System;
using System.Collections.Generic;
using System.Text;
using MarginTrading.MarketMaker.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MarginTrading.MarketMaker.Models.Api
{
    public class ExtPriceStatusModel
    {
        public string Exchange { get; set; }
        public BestPricesModel BestPrices { get; set; }
        public decimal HedgingPreference { get; set; }
        public bool OrderbookReceived { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ExchangeErrorState? Error { get; set; }
    }
}
