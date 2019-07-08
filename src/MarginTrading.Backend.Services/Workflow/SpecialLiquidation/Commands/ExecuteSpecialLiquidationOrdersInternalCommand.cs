// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands
{
    [MessagePackObject]
    public class ExecuteSpecialLiquidationOrdersInternalCommand
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(3)]
        public string Instrument { get; set; }
        
        [Key(4)]
        public decimal Volume { get; set; }
        
        [Key(5)]
        public decimal Price { get; set; }
        
        [Key(6)]
        public string MarketMakerId { get; set; }
        
        [Key(7)]
        public string ExternalOrderId { get; set; }
        
        [Key(8)]
        public DateTime ExternalExecutionTime { get; set; }
    }
}