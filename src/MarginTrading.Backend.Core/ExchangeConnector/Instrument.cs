// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;

namespace MarginTrading.Backend.Core.ExchangeConnector
{
    public class Instrument
    {
        /// <summary>Initializes a new instance of the Instrument class.</summary>
        public Instrument()
        {
        }

        /// <summary>Initializes a new instance of the Instrument class.</summary>
        public Instrument(string name = null, string exchange = null, string baseProperty = null, string quote = null)
        {
            this.Name = name;
            this.Exchange = exchange;
            this.BaseProperty = baseProperty;
            this.Quote = quote;
        }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; private set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "exchange")]
        public string Exchange { get; private set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "base")]
        public string BaseProperty { get; private set; }

        /// <summary>
        /// </summary>
        [JsonProperty(PropertyName = "quote")]
        public string Quote { get; private set; }
    }
}