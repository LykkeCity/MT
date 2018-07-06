using System;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    public class WithdrawalCommandsHandler
    {
        private readonly IConvertService _convertService;
        private readonly IDateService _dateService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IAccountUpdateService _accountUpdateService;

        public WithdrawalCommandsHandler(IConvertService convertService,
            IDateService dateService,
            IAccountsCacheService accountsCacheService,
            IAccountUpdateService accountUpdateService)
        {
            _convertService = convertService;
            _dateService = dateService;
            _accountsCacheService = accountsCacheService;
            _accountUpdateService = accountUpdateService;
        }

        /// <summary>
        /// Freeze the the amount in the margin.
        /// </summary>
        [UsedImplicitly]
        private void Handle(FreezeAmountForWithdrawalCommand command, IEventPublisher publisher)
        {
            MarginTradingAccount account = null;
            try
            {
                account = _accountsCacheService.Get(command.AccountId);
            }
            catch
            {
                publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.OperationId, _dateService.Now(), 
                    command.ClientId, command.AccountId, command.Amount, $"Failed to get account {command.AccountId}"));
                return;
            }
            
            if (account.GetFreeMargin() >= command.Amount)
            {
                _accountUpdateService.FreezeWithdrawalMargin(command.AccountId, command.OperationId, command.Amount);
                
                publisher.PublishEvent(new AmountForWithdrawalFrozenEvent(command.OperationId, _dateService.Now(), 
                    command.ClientId, command.AccountId, command.Amount, command.Reason));
            }
            else
            {
                publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.OperationId, _dateService.Now(), 
                    command.ClientId, command.AccountId, command.Amount, "Not enough free margin"));
            }
        }
        
        /// <summary>
        /// Withdrawal failed => margin must be unfrozen.
        /// </summary>
        [UsedImplicitly]
        private void Handle(UnfreezeMarginOnFailWithdrawalCommand command, IEventPublisher publisher)
        {
            //errors not handled => if error occurs event will be retried
            _accountUpdateService.UnfreezeWithdrawalMargin(command.AccountId, command.OperationId);
            
            publisher.PublishEvent(new UnfreezeMarginOnFailSucceededWithdrawalEvent(command.OperationId, _dateService.Now(), 
                command.ClientId, command.AccountId, command.Amount));
        }
        
        /// <summary>
        /// Withdrawal succeeded => margin must be unfrozen.
        /// </summary>
        [UsedImplicitly]
        private void Handle(UnfreezeMarginWithdrawalCommand command, IEventPublisher publisher)
        {
        }
    }
}