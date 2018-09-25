using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
    public class SpecialLiquidationOperationData
    {
        public SpecialLiquidationOperationState State { get; set; }
        
        public string Instrument { get; set; }
        public List<string> PositionIds { get; set; }
        public decimal Volume { get; set; }
        public decimal Price { get; set; }
        public string ExternalProviderId { get; set; }
    }
}