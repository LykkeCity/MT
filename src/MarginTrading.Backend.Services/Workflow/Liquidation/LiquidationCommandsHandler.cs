using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;

namespace MarginTrading.Backend.Services.Workflow.Liquidation
{
    [UsedImplicitly]
    public class LiquidationCommandsHandler
    {
        public LiquidationCommandsHandler()
        {
            
        }

        private async Task Handle(StartLiquidationInternalCommand command, IEventPublisher publisher)
        {
            
        }
        
    }
}