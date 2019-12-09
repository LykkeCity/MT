// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.TradingSchedule
{
    [MessagePackObject]
    public class MarketStateChangedEvent
    {
        [Key(0)]
        public string Id { get; set; }

        [Key(1)]
        public bool IsEnabled { get; set; }

        [Key(2)]
        public DateTime EventTimestamp { get; set; }
    }
}