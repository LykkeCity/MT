// Copyright (c) 2019 Lykke Corp.

using System;
using MarginTrading.Backend.Core;

namespace MarginTrading.Backend.Services.Events
{
    public class FxBestPriceChangeEventArgs
    {
        public FxBestPriceChangeEventArgs(InstrumentBidAskPair pair)
        {
            BidAskPair = pair ?? throw new ArgumentNullException(nameof(pair));
        }

        public InstrumentBidAskPair BidAskPair { get; }
    }
}