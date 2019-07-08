// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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