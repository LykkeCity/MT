using System;

namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain
{
    public class ExchangeBestPrice
    {
        public string ExchangeName { get; set; }
        
        public string Instrument { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        public decimal Bid { get; set; }
        
        public decimal Ask { get; set; }
    }
}
