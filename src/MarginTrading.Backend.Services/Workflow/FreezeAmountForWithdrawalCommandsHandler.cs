using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Commands;
using MarginTrading.Backend.Contracts.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    /// <summary>
    /// Freezes amount for withdrawal
    /// </summary>
    public class FreezeAmountForWithdrawalCommandsHandler
    {
        private readonly IConvertService _convertService;
        private readonly IAccountsCacheService _accountsCacheService;

        public FreezeAmountForWithdrawalCommandsHandler(IConvertService convertService,
            IAccountsCacheService accountsCacheService)
        {
            _convertService = convertService;
            _accountsCacheService = accountsCacheService;
        }

        /// <summary>
        /// Freeze the the amount in the margin.
        /// </summary>
        [UsedImplicitly]
        private void Handle(FreezeAmountForWithdrawalCommand command, IEventPublisher publisher)
        {
            var account = _accountsCacheService.Get(command.AccountId);
            if (account.GetFreeMargin() <= command.Amount)
            {
                // todo: check condition
                // todo: add actual amount freezing (see MTC-117)
                publisher.PublishEvent(_convertService.Convert<AmountForWithdrawalFrozenEvent>(command));
            }
            else
                publisher.PublishEvent(new AmountForWithdrawalFreezeFailedEvent(command.ClientId, command.AccountId,
                    command.Amount, command.OperationId, "Not enough free margin"));
        }
    }
}