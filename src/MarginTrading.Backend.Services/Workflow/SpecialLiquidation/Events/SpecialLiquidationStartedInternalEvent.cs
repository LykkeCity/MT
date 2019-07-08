// Copyright (c) 2019 Lykke Corp.

using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events
{
    [MessagePackObject]
    public class SpecialLiquidationStartedInternalEvent
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(2)]
        public string Instrument { get; set; }
    }
}