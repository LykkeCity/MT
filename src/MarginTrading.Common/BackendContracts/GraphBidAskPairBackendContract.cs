using System;

namespace MarginTrading.Common.BackendContracts
{
    public class GraphBidAskPairBackendContract
    {
        public double Bid { get; set; }
        public double Ask { get; set; }
        public DateTime Date { get; set; }
    }
}
