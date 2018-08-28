using System;
using MessagePack;

namespace MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands
{
    [MessagePackObject]
    public class GetPriceForSpecialLiquidationCommand
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public DateTime CreationTime { get; set; }
        
        [Key(3)]
        public string Instrument { get; set; }
        
        [Key(4)]
        public decimal Volume { get; set; }
    }
}