using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    public class WithdrawalCommandsHandler
    {
        private readonly IDateService _dateService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private const string OperationName = "FreezeAmountForWithdrawal";

        public WithdrawalCommandsHandler(
            IDateService dateService,
            IAccountsCacheService accountsCacheService,
            IAccountUpdateService accountUpdateService,
            IChaosKitty chaosKitty,
            IOperationExecutionInfoRepository operationExecutionInfoRepository)
        {
            _dateService = dateService;
            _accountsCacheService = accountsCacheService;
            _accountUpdateService = accountUpdateService;
            _chaosKitty = chaosKitty;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
        }

        /// <summary>
        /// Freeze the the amount in the margin.
        /// </summary>
        [UsedImplicitly]
        private async Task Handle(FreezeAmountForWithdrawalCommand command, IEventPublisher publisher)
        {
            //ensure idempotency
            var executionInfo = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: OperationName,
                operationId: command.OperationId,
                factory: () => new OperationExecutionInfo<WithdrawalFreezeOperationData>(
                    operationName: OperationName,
                    id: command.OperationId,
                    lastModified: _dateService.Now(),
                    data: new WithdrawalFreezeOperationData
                    {
                        State = OperationState.Initiated,
                        AccountId = command.AccountId,
                        Amount = command.Amount,
                    }
                ));
            
            MarginTradingAccount account = null;
            try
            {
                account = _accountsCacheService.Get(command.AccountId);
            }
            catch
            {
                publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.OperationId, _dateService.Now(), 
                    command.AccountId, command.Amount, $"Failed to get account {command.AccountId}"));
                return;
            }

            if (executionInfo.Data.SwitchState(OperationState.Initiated, OperationState.Started))
            {
                if (account.GetFreeMargin() >= command.Amount)
                {
                    await _accountUpdateService.FreezeWithdrawalMargin(command.AccountId, command.OperationId,
                        command.Amount);
                    
                    _chaosKitty.Meow(command.OperationId);

                    publisher.PublishEvent(new AmountForWithdrawalFrozenEvent(command.OperationId, _dateService.Now(),
                        command.AccountId, command.Amount, command.Reason));
                }
                else
                {
                    publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.OperationId,
                        _dateService.Now(),
                        command.AccountId, command.Amount, "Not enough free margin"));
                }
                
                _chaosKitty.Meow(command.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
        
        /// <summary>
        /// Withdrawal failed => margin must be unfrozen.
        /// </summary>
        /// <remarks>Errors are not handled => if error occurs event will be retried</remarks>
        [UsedImplicitly]
        private async Task Handle(UnfreezeMarginOnFailWithdrawalCommand command, IEventPublisher publisher)
        {
            //ensure operation idempotency
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<WithdrawalFreezeOperationData>(
                operationName: OperationName,
                id: command.OperationId
            );

            // ReSharper disable once PossibleNullReferenceException
            if (executionInfo.Data.SwitchState(OperationState.Started, OperationState.Finished))
            {
                await _accountUpdateService.UnfreezeWithdrawalMargin(executionInfo.Data.AccountId, command.OperationId);

                _chaosKitty.Meow(command.OperationId);
                
                publisher.PublishEvent(new UnfreezeMarginOnFailSucceededWithdrawalEvent(command.OperationId,
                    _dateService.Now(), executionInfo.Data.AccountId, executionInfo.Data.Amount));
                
                _chaosKitty.Meow(command.OperationId);
                
                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
    }
}