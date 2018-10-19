using System.Threading.Tasks;
using AutoMapper;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    /// <summary>
    /// Listens to <see cref="AccountChangedEvent"/>s and builds a projection inside of the
    /// <see cref="IAccountsCacheService"/>
    /// </summary>
    public class AccountsProjection
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IClientNotifyService _clientNotifyService;
        private readonly IEventChannel<AccountBalanceChangedEventArgs> _accountBalanceChangedEventChannel;
        private readonly IConvertService _convertService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly IDateService _dateService;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly IChaosKitty _chaosKitty;
        private readonly OrdersCache _ordersCache;
        private readonly ILog _log;

        private const string OperationName = "AccountsProjection";
        
        public AccountsProjection(
            IAccountsCacheService accountsCacheService, 
            IClientNotifyService clientNotifyService,
            IEventChannel<AccountBalanceChangedEventArgs> accountBalanceChangedEventChannel,
            IConvertService convertService, 
            IAccountUpdateService accountUpdateService,
            IDateService dateService,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            IChaosKitty chaosKitty,
            OrdersCache ordersCache, 
            ILog log)
        {
            _accountsCacheService = accountsCacheService;
            _clientNotifyService = clientNotifyService;
            _accountBalanceChangedEventChannel = accountBalanceChangedEventChannel;
            _convertService = convertService;
            _accountUpdateService = accountUpdateService;
            _dateService = dateService;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _chaosKitty = chaosKitty;
            _ordersCache = ordersCache;
            _log = log;
        }

        /// <summary>
        /// CQRS projection impl
        /// </summary>
        [UsedImplicitly]
        public async Task Handle(AccountChangedEvent e)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetOrAddAsync(
                operationName: OperationName,
                operationId: e.OperationId,
                factory: () => new OperationExecutionInfo<OperationData>(
                    operationName: OperationName,
                    id: e.OperationId,
                    lastModified: _dateService.Now(),
                    data: new OperationData { State = OperationState.Initiated }
                ));

            if (executionInfo.Data.SwitchState(OperationState.Initiated, OperationState.Finished))
            {
                var updatedAccount = Convert(e.Account);

                switch (e.EventType)
                {
                    case AccountChangedEventTypeContract.Created:
                        _accountsCacheService.TryAddNew(MarginTradingAccount.Create(updatedAccount));
                        break;
                    case AccountChangedEventTypeContract.Updated:
                    {
                        var account = _accountsCacheService.TryGet(e.Account.Id);
                        if (await ValidateAccount(account, e)
                            && await _accountsCacheService.UpdateAccountChanges(updatedAccount.Id,
                                updatedAccount.TradingConditionId, updatedAccount.WithdrawTransferLimit,
                                updatedAccount.IsDisabled, updatedAccount.IsWithdrawalDisabled, e.ChangeTimestamp))
                        {
                            _accountUpdateService.RemoveLiquidationStateIfNeeded(e.Account.Id,
                                "Trading conditions changed");
                            _clientNotifyService.NotifyAccountUpdated(updatedAccount);
                        }
                        break;
                    }
                    case AccountChangedEventTypeContract.BalanceUpdated:
                    {
                        if (e.BalanceChange != null)
                        {
                            var account = _accountsCacheService.TryGet(e.Account.Id);
                            if (await ValidateAccount(account, e))
                            {
                                if (await _accountsCacheService.UpdateAccountBalance(updatedAccount.Id,
                                    e.BalanceChange.Balance, e.ChangeTimestamp))
                                {
                                    _accountUpdateService.RemoveLiquidationStateIfNeeded(e.Account.Id,
                                        "Balance updated");
                                    _accountBalanceChangedEventChannel.SendEvent(this,
                                        new AccountBalanceChangedEventArgs(updatedAccount));
                                }
                                
                                switch (e.BalanceChange.ReasonType)
                                {
                                    case AccountBalanceChangeReasonTypeContract.Withdraw:
                                        await _accountUpdateService.UnfreezeWithdrawalMargin(updatedAccount.Id,
                                            e.BalanceChange.Id);
                                        break;
                                    case AccountBalanceChangeReasonTypeContract.UnrealizedDailyPnL:
                                        if (_ordersCache.Positions.TryGetPositionById(e.BalanceChange.EventSourceId,
                                            out var position))
                                        {
                                            position.ChargePnL(e.BalanceChange.Id, e.BalanceChange.ChangeAmount);
                                        }
                                        else
                                        {
                                            _log.WriteWarning("AccountChangedEvent Handler", e.ToJson(),
                                                $"Position [{e.BalanceChange.EventSourceId} was not found]");
                                        }
                                        break;
                                    case AccountBalanceChangeReasonTypeContract.RealizedPnL:
                                        await _accountUpdateService.UnfreezeUnconfirmedMargin(e.Account.Id, 
                                            e.BalanceChange.EventSourceId);
                                        break;
                                }
                            }
                        }
                        else
                        {
                            _log.WriteWarning("AccountChangedEvent Handler", e.ToJson(), "BalanceChange info is empty");
                        }

                        break;
                    }
                }
                
                _chaosKitty.Meow(e.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }

        private async Task<bool> ValidateAccount(IMarginTradingAccount account, AccountChangedEvent e)
        {
            if (account == null)
            {
                await _log.WriteWarningAsync(nameof(AccountsProjection), e.ToJson(),
                    $"Account with id {e.Account.Id} was not found");
                return false;
            }

            return true;
        }

        private MarginTradingAccount Convert(AccountContract accountContract)
        {
            return _convertService.Convert<AccountContract, MarginTradingAccount>(accountContract,
                o => o.ConfigureMap(MemberList.Source).ForSourceMember(x => x.ModificationTimestamp, c => c.Ignore()));
        }
    }
}