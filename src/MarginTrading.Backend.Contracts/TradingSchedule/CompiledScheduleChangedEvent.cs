// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using MessagePack;

namespace MarginTrading.Backend.Contracts.TradingSchedule
{
    [MessagePackObject]
    public class CompiledScheduleChangedEvent
    {
        [Key(0)]
        public string AssetPairId { get; set; }
        
        [Key(1)]
        public DateTime EventTimestamp { get; set; }

        [Key(2)]
        public List<CompiledScheduleTimeIntervalContract> TimeIntervals { get; set; }
    }
}