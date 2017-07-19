using System;

namespace MarginTrading.Common.BackendContracts
{
    public class InstrumentBidAskPairContract
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
    }
}
