using System;
using MarginTrading.Backend.Core.FakeExchangeConnector.Caches;

namespace MarginTrading.Backend.Core.FakeExchangeConnector.Domain
{
    public class ExchangeInstrumentQuote : IDoubleKeyedObject, ICloneable
    {
        public ExchangeInstrumentQuote()
        {
            Timestamp = DateTime.UtcNow;
        }
        
        public string ExchangeName { get; set; }
        
        public string Instrument { get; set; }
        
        public string @Base { get; set; }
        
        public string Quote { get; set; }
        
        public decimal Bid { get; set; }
        
        public decimal Ask { get; set; }
        
        public DateTime Timestamp { get; private set; }

        public string PartitionKey => ExchangeName;

        public string RowKey => Instrument;
        
        public object Clone()
        {
            return new ExchangeInstrumentQuote
            {
                ExchangeName = this.ExchangeName,
                Instrument = this.Instrument,
                Base = this.Base,
                Quote = this.Quote,
                Bid = this.Bid,
                Ask = this.Ask,
                Timestamp = this.Timestamp
            };
        }
    }
}
