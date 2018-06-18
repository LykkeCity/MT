using System;
using MarginTrading.Backend.Contracts.Positions;

namespace MarginTrading.Backend.Contracts.Events
{
    public class PositionHistoryEvent
    {
        public PositionContract PositionSnapshot { get; set; }
        public DealContract Deal { get; set; }
        public PositionHistoryTypeContract EventType { get; set; }
        public DateTime Timestamp { get; set; }
    }
}