using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Commands;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    public class WithdrawalCommandsHandler
    {
        private readonly IConvertService _convertService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IAccountUpdateService _accountUpdateService;

        public WithdrawalCommandsHandler(IConvertService convertService,
            IAccountsCacheService accountsCacheService,
            IAccountUpdateService accountUpdateService)
        {
            _convertService = convertService;
            _accountsCacheService = accountsCacheService;
            _accountUpdateService = accountUpdateService;
        }

        /// <summary>
        /// Freeze the the amount in the margin.
        /// </summary>
        [UsedImplicitly]
        private void Handle(FreezeAmountForWithdrawalCommand command, IEventPublisher publisher)
        {
            var account = _accountsCacheService.Get(command.AccountId);
            if (account.GetFreeMargin() >= command.Amount)
            {
                // todo: check condition
                // todo: add actual amount freezing (see MTC-117)
                _accountUpdateService.FreezeWithdrawalMargin(account, command.OperationId, command.Amount);
                
                publisher.PublishEvent(_convertService.Convert<AmountForWithdrawalFrozenEvent>(command));
            }
            else
            {
                publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.ClientId, command.AccountId,
                    command.Amount, command.OperationId, "Not enough free margin"));
            }
        }

        /// <summary>
        /// No matter if withdrawal succeeded of failed the margin must be unfrozen.
        /// </summary>
        [UsedImplicitly]
        private void Handle(UnfreezeMarginWithdrawalCommand command, IEventPublisher publisher)
        {
            var account = _accountsCacheService.Get(command.AccountId);
            _accountUpdateService.UnfreezeWithdrawalMargin(account, command.OperationId);
            
            publisher.PublishEvent(new UnfreezeMarginSucceededWithdrawalEvent(command.OperationId, command.ClientId, 
                command.AccountId, command.Amount));
        }
    }
}