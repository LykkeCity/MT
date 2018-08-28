using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands
{
    [MessagePackObject]
    public class ExecuteSpecialLiquidationOrderCommand
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
    }
}