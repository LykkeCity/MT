using System;
using System.Collections.Generic;
using System.Text;

namespace MarginTrading.MarketMaker.Messages
{
    public class StopOrAllowNewTradesMessage
    {
        public string AssetPairId { get; set; }
        public string MarketMakerId { get; set; }
        public string Reason { get; set; }
        public bool Stop { get; set; }
    }
}
