using System;

namespace MarginTrading.Backend.Core.DayOffSettings
{
    public class DayOffExclusion
    {
        public Guid Id { get; }
        public string AssetPairRegex { get; }
        public DateTime Start { get; }
        public DateTime End { get; }
        public bool IsTradeEnabled { get; }

        public DayOffExclusion(Guid id, string assetPairRegex, DateTime start, DateTime end, bool isTradeEnabled)
        {
            Id = id;
            AssetPairRegex = assetPairRegex ?? throw new ArgumentNullException(nameof(assetPairRegex));
            Start = start;
            End = end;
            IsTradeEnabled = isTradeEnabled;
        }
    }
}