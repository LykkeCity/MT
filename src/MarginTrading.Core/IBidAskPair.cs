using System;

namespace MarginTrading.Core
{
    public interface IBidAskPair
    {
        double Bid { get; }
        double Ask { get; }
    }

    public class BidAskPair : IBidAskPair
    {
        public double Bid { get; set; }
        public double Ask { get; set; }
    }

    public class GraphBidAskPair : IBidAskPair
    {
        public double Bid { get; set; }
        public double Ask { get; set; }
        public DateTime Date { get; set; }
    }

    public class InstrumentBidAskPair : BidAskPair
    {
        public string Instrument { get; set; }
        public DateTime Date { get; set; }
    }

    public static class BidAskPairExtetsion
    {
        public static double GetPriceForOrderType(this IBidAskPair bidAskPair, OrderDirection orderType)
        {
            return orderType == OrderDirection.Buy ? bidAskPair.Bid : bidAskPair.Ask;
        }
    }
}
