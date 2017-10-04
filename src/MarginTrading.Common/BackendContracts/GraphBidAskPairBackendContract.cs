using System;

namespace MarginTrading.Common.BackendContracts
{
    public class GraphBidAskPairBackendContract
    {
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public DateTime Date { get; set; }
    }
}
