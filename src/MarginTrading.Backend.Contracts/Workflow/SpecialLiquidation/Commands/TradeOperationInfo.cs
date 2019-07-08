// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands
{
    public class TradeOperationInfo
    {
        public string OperationId { get; set; }
        
        public int RequestNumber { get; set; }
    }
}