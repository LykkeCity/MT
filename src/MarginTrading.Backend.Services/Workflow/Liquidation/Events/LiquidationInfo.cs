using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.Liquidation.Events
{
    [MessagePackObject]
    public class LiquidationInfo
    {
        [Key(0)]
        public string PositionId {get; set; }
            
        [Key(1)]
        public bool IsLiquidated { get; set; }
            
        [Key(2)]
        public string Comment { get; set; }
    }
}