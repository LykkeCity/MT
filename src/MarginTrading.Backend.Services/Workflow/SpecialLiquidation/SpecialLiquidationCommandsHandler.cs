using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation
{
    [UsedImplicitly]
    public class SpecialLiquidationCommandsHandler
    {
        private readonly 
        
        public SpecialLiquidationCommandsHandler()
        {
            
        }

        [UsedImplicitly]
        private async Task Handle(StartSpecialLiquidationInternalCommand command, IEventPublisher publisher)
        {
            
        }
    }
}