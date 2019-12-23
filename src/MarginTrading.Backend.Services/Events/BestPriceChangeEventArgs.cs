// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public class BestPriceChangeEventArgs
    {
        public BestPriceChangeEventArgs(InstrumentBidAskPair pair, bool isEod = false)
        {
            BidAskPair = pair ?? throw new ArgumentNullException(nameof(pair));
            IsEod = isEod;
        }

        public InstrumentBidAskPair BidAskPair { get; }
        
        public bool IsEod { get; }
    }
}