using System;
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
        public string Instrument { get; set; }
        public DateTime Date { get; set; }
    }

    public static class BidAskPairExtension
    {
        public static decimal GetPriceForOrderDirection(this IBidAskPair bidAskPair, OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? bidAskPair.Ask : bidAskPair.Bid;
        }
    }
}
