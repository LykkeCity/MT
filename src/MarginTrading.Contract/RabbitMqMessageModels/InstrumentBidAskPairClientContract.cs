// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Newtonsoft.Json;

namespace MarginTrading.Contract.RabbitMqMessageModels
{
    public class BidAskPairRabbitMqContract
    {
        public string Instrument { get; set; }
        public DateTime Date { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsEod { get; set; }
    }
}