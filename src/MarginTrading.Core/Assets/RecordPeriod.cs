using System;
using System.Collections.Generic;

namespace MarginTrading.Core.Assets
{
    public class PeriodRecord
    {
        public DateTime? FixingTime { get; set; }
        public List<decimal> Changes { get; set; }
    }

    public class AskBid
    {
        public decimal A { get; set; }
        public decimal B { get; set; }
    }

    public class AskBidPeriodRecord
    {
        public DateTime? FixingTime { get; set; }
        public List<AskBid> Changes { get; set; }
    }
}
