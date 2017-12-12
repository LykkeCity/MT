using System;

namespace MarginTrading.Contract.BackendContracts.DayOffSettings
{
    public class DayOffExclusionContract
    {
        public Guid Id { get; set; }
        public string AssetPairRegex { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public bool IsTradeEnabled { get; set; }
    }
}