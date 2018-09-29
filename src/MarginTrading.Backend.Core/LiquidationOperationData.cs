using System.Collections.Generic;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
    public class LiquidationOperationData : OperationDataBase<LiquidationOperationState>
    {
        public string AccountId { get; set; }
        public string AssetPairId { get; set; }
        public PositionDirection? Direction { get; set; }
        public List<string> LiquidatedPositionIds { get; set; }
    }
}