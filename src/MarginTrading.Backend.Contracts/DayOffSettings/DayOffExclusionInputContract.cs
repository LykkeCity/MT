using System;

namespace MarginTrading.Backend.Contracts.DayOffSettings
{
    public class DayOffExclusionInputContract
    {
        public string AssetPairRegex { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsTradeEnabled { get; set; }
    }
}