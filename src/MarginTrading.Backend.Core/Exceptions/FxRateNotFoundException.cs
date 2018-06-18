using System;

namespace MarginTrading.Backend.Core.Exceptions
{
    public class FxRateNotFoundException : Exception
    {
        public string InstrumentId { get; private set; }

        public FxRateNotFoundException(string instrumentId, string message):base(message)
        {
            InstrumentId = instrumentId;
        }
    }
}