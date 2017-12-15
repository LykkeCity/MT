using System;

namespace MarginTrading.Backend.Contracts.DayOffSettings
{
    public class CompiledExclusionContract
    {
        public Guid Id { get; set; }
        public string AssetPairRegex { get; set; }
        public string AssetPairId { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}