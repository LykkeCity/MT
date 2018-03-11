using System;
using System.Collections.Generic;

namespace MarginTrading.Backend.Core.Orderbooks
{
    public class ExternalOrderBook
    {
        public string ExchangeName { get; set; }

        public string AssetPairId { get; set; }

        public DateTime Timestamp { get; set; }

        public List<VolumePrice> Asks { get; set; }

        public List<VolumePrice> Bids { get; set; }
    }
}