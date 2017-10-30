using System;

namespace MarginTrading.Contract.ClientContracts
{
    public class GraphBidAskPairClientContract
    {
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public DateTime Date { get; set; }
    }
}
