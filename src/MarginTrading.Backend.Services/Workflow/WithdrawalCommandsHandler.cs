using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
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
        /// Withdrawal failed => margin must be unfrozen.
        /// </summary>
        [UsedImplicitly]
        private void Handle(UnfreezeMarginOnFailWithdrawalCommand command, IEventPublisher publisher)
        {
            var account = _accountsCacheService.Get(command.AccountId);
            _accountUpdateService.UnfreezeWithdrawalMargin(account, command.OperationId);
            
            publisher.PublishEvent(new UnfreezeMarginSucceededWithdrawalEvent(command.OperationId, command.ClientId, 
                command.AccountId, command.Amount));
        }
        
        /// <summary>
        /// Withdrawal succeeded => margin must be unfrozen.
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