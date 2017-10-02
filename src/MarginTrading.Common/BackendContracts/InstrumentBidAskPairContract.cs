using System;

namespace MarginTrading.Common.BackendContracts
{
    public class InstrumentBidAskPairContract
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
    }
}
