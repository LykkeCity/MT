// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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