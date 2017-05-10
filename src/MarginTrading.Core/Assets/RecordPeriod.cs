using System;
using System.Collections.Generic;

namespace MarginTrading.Core.Assets
{
    public class PeriodRecord
    {
        public DateTime? FixingTime { get; set; }
        public List<double> Changes { get; set; }
    }

    public class AskBid
    {
        public double A { get; set; }
        public double B { get; set; }
    }

    public class AskBidPeriodRecord
    {
        public DateTime? FixingTime { get; set; }
        public List<AskBid> Changes { get; set; }
    }
}
