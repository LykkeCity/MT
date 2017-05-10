using System;

namespace MarginTrading.Common.ClientContracts
{
    public class GraphBidAskPairClientContract
    {
        public double Bid { get; set; }
        public double Ask { get; set; }
        public DateTime Date { get; set; }
    }
}
