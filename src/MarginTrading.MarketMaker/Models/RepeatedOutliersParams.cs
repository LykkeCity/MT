using System;

namespace MarginTrading.MarketMaker.Models
{
    public class RepeatedOutliersParams
    {
        public int MaxSequenceLength { get; }
        public TimeSpan MaxSequenceAge { get; }
        public decimal MaxAvg { get; }
        public TimeSpan MaxAvgAge { get; }

        public RepeatedOutliersParams(int maxSequenceLength, TimeSpan maxSequenceAge, decimal maxAvg, TimeSpan maxAvgAge)
        {
            MaxSequenceLength = maxSequenceLength;
            MaxSequenceAge = maxSequenceAge;
            MaxAvg = maxAvg;
            MaxAvgAge = maxAvgAge;
        }
    }
}
