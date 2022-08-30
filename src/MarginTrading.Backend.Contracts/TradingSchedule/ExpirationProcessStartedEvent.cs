// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.TradingSchedule
{
    [MessagePackObject()]
    public class ExpirationProcessStartedEvent
    {
        [Key(0)]
        public DateTime OperationIntervalEnd { get; set; }
    }
}