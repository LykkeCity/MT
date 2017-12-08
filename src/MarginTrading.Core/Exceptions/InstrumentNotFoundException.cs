using System;

namespace MarginTrading.Core.Exceptions
{
    public class InstrumentNotFoundException : Exception
    {
        public string InstrumentId { get; private set; }

        public InstrumentNotFoundException(string instrumentId, string message):base(message)
        {
            InstrumentId = instrumentId;
        }
    }
}