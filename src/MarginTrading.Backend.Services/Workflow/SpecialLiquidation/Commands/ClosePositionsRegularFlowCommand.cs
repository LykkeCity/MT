// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MessagePack;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands
{
    [MessagePackObject()]
    public class ClosePositionsRegularFlowCommand
    {
        [Key(0)]
        public string OperationId { get; set; }
    }
}