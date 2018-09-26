using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.Liquidation.Events;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow.Liquidation
{
    [UsedImplicitly]
    public class LiquidationCommandsHandler
    {
        private readonly IAccountsCacheService _accountsCache;
        private readonly IDateService _dateService;

        public LiquidationCommandsHandler(IAccountsCacheService accountsCache,
            IDateService dateService)
        {
            _accountsCache = accountsCache;
            _dateService = dateService;
        }

        private async Task Handle(StartLiquidationInternalCommand command, IEventPublisher publisher)
        {
            if (_accountsCache.TryGet(command.AccountId) == null)
            {
                publisher.PublishEvent(new LiquidationFailedInternalEvent
                {
                    OperationId = command.OperationId, 
                    CreationTime = _dateService.Now(),
                    Reason = "Account does not exist"
                });

                return;
            }
            
            if (!_accountsCache.TryStartLiquidation(command.AccountId))
            {
                publisher.PublishEvent(new LiquidationFailedInternalEvent
                {
                    OperationId = command.OperationId, 
                    CreationTime = _dateService.Now(),
                    Reason = "Liquidation is already in progress"
                });

                return;
            }
            
            
        }
        
    }
}