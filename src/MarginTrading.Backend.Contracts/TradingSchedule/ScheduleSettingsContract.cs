using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.TradingSchedule
{
    [MessagePackObject]
    public class ScheduleSettingsContract
    {
        [Key(0)]
        public string Id { get; set; }
        
        [Key(1)]
        public int Rank { get; set; }
        
        [Key(2)]
        public bool? IsTradeEnabled { get; set; } = false;
        
        [Key(3)]
        public TimeSpan? PendingOrdersCutOff { get; set; }
    }
}