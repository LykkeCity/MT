using System;

namespace MarginTrading.Common.ClientContracts
{
    public class GraphBidAskPairClientContract
    {
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public DateTime Date { get; set; }
    }
}
