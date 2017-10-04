using System;

namespace MarginTrading.Core
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

    public class GraphBidAskPair : IBidAskPair
    {
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public DateTime Date { get; set; }
    }

    public class InstrumentBidAskPair : BidAskPair
    {
        public string Instrument { get; set; }
        public DateTime Date { get; set; }
    }

    public static class BidAskPairExtetsion
    {
        public static decimal GetPriceForOrderType(this IBidAskPair bidAskPair, OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? bidAskPair.Bid : bidAskPair.Ask;
        }
    }
}
