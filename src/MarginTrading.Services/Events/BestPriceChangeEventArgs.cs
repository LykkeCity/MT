using System;
using MarginTrading.Backend.Core;

namespace MarginTrading.Services.Events
{
    public class BestPriceChangeEventArgs
    {
        public BestPriceChangeEventArgs(InstrumentBidAskPair pair)
        {
            if (pair == null) throw new ArgumentNullException(nameof(pair));
            BidAskPair = pair;
        }

        public InstrumentBidAskPair BidAskPair { get; }
    }
}