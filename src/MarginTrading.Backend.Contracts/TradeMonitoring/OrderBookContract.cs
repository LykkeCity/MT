using System.Collections.Generic;

namespace MarginTrading.Backend.Contracts.TradeMonitoring
{
    public class OrderBookContract
    {
        public string Instrument { get; set; }
        public List<LimitOrderContract> Buy { get; set; }
        public List<LimitOrderContract> Sell { get; set; }
    }
}
