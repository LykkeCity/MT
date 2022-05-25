// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Lykke.Snow.Common.Quotes;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public interface IBidAskPair
    {
        decimal Bid { get; }
        decimal Ask { get; }
    }

    public class BidAskPair : IBidAskPair
    {
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
    }

    public class InstrumentBidAskPair : BidAskPair
    {
        public decimal BidFirstLevelVolume { get; set; }
        public decimal AskFirstLevelVolume { get; set; }
        public string Instrument { get; set; }
        public DateTime Date { get; set; }
    }

    public static class BidAskPairExtension
    {
        public static decimal GetPriceForOrderDirection(this InstrumentBidAskPair bidAskPair, OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? bidAskPair.Ask : bidAskPair.Bid;
        }
        
        public static decimal GetVolumeForOrderDirection(this InstrumentBidAskPair bidAskPair, OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? bidAskPair.AskFirstLevelVolume : bidAskPair.BidFirstLevelVolume;
        }

        public static Quote ToMathModel(this InstrumentBidAskPair bidAskPair) =>
            new Quote(new AskPrice(bidAskPair.Ask), new BidPrice(bidAskPair.Bid));
    }
}
