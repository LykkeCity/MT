using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Events;

namespace MarginTrading.Backend.Services.Workflow.SpecialLiquidation
{
    [UsedImplicitly]
    public class SpecialLiquidationSaga
    {


        public SpecialLiquidationSaga()
        {
            
        }

        [UsedImplicitly]
        private async Task Handle(SpecialLiquidationStartedInternalEvent e, ICommandSender sender)
        {
            
        }
    }
}