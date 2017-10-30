using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class QuoteNotFoundException : Exception
    {
        public string InstrumentId { get; private set; }

        public QuoteNotFoundException(string instrumentId, string message):base(message)
        {
            InstrumentId = instrumentId;
        }
    }
}