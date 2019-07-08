// Copyright (c) 2019 Lykke Corp.

using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.TradingSchedule
{
    [MessagePackObject]
    public class CompiledScheduleTimeIntervalContract
    {
        [Key(0)]
        public ScheduleSettingsContract Schedule { get; set; }
        
        [Key(1)]
        public DateTime Start { get; set; }
        
        [Key(2)]
        public DateTime End { get; set; }
    }
}