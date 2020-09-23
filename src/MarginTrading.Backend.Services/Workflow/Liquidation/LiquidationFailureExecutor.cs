// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Contracts.Workflow.Liquidation;
using MarginTrading.Backend.Contracts.Workflow.Liquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow.Liquidation
{
    public class LiquidationFailureExecutor : ILiquidationFailureExecutor
    {
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly IAccountsCacheService _accountsCache;
        private readonly IDateService _dateService;
        private readonly OrdersCache _ordersCache;
        private readonly IEventChannel<LiquidationEndEventArgs> _liquidationEndEventChannel;
        private readonly ILog _log;

        public LiquidationFailureExecutor(
            IOperationExecutionInfoRepository operationExecutionInfoRepository, 
            IAccountsCacheService accountsCache,
            IDateService dateService,
            OrdersCache ordersCache,
            IEventChannel<LiquidationEndEventArgs> liquidationEndEventChannel,
            ILog log)
        {
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _accountsCache = accountsCache;
            _dateService = dateService;
            _ordersCache = ordersCache;
            _liquidationEndEventChannel = liquidationEndEventChannel;
            _log = log;
        }

        public async Task ExecuteAsync(IEventPublisher failurePublisher, string accountId, string operationId, string reason)
        {
            if (failurePublisher == null)
                throw new ArgumentNullException(nameof(failurePublisher));
            
            if (string.IsNullOrEmpty(accountId))
                throw new ArgumentNullException(nameof(accountId));
            
            if (string.IsNullOrEmpty(operationId))
                throw new ArgumentNullException(nameof(operationId));
            
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<LiquidationOperationData>(
                LiquidationSaga.OperationName,
                operationId);
            
            if (executionInfo?.Data == null)
            {
                await _log.WriteWarningAsync(nameof(LiquidationFailureExecutor),
                    nameof(ExecuteAsync),
                    new {operationId, accountId}.ToJson(),
                    $"Unable to execute failure. Liquidation execution info was not found.");
                return;
            }

            if (!_accountsCache.TryFinishLiquidation(accountId, reason, operationId))
            {
                await _log.WriteWarningAsync(nameof(LiquidationFailureExecutor),
                    nameof(ExecuteAsync),
                    new {operationId, accountId}.ToJson(),
                    $"Unable to execute failure. Couldn't finish liquidation.");

                throw new InvalidOperationException(
                    "Liquidation can't be failed because it has not been finished explicitly");
            }
            
            var account = _accountsCache.Get(accountId);

            failurePublisher.PublishEvent(new LiquidationFailedEvent
            {
                OperationId = operationId,
                CreationTime = _dateService.Now(),
                Reason = reason,
                LiquidationType = executionInfo.Data.LiquidationType.ToType<LiquidationTypeContract>(),
                AccountId = executionInfo.Data.AccountId,
                AssetPairId = executionInfo.Data.AssetPairId,
                Direction = executionInfo.Data.Direction?.ToType<PositionDirectionContract>(),
                QuoteInfo = executionInfo.Data.QuoteInfo,
                ProcessedPositionIds = executionInfo.Data.ProcessedPositionIds,
                LiquidatedPositionIds = executionInfo.Data.LiquidatedPositionIds,
                OpenPositionsRemainingOnAccount = _ordersCache.Positions.GetPositionsByAccountIds(executionInfo.Data.AccountId).Count,
                CurrentTotalCapital = account.GetTotalCapital(),
            });

            await _log.WriteInfoAsync(nameof(LiquidationFailureExecutor),
                nameof(ExecuteAsync),
                new {account, operationId, reason}.ToJson(),
                $"Successfully published {nameof(LiquidationFailedEvent)} event");
            
            _liquidationEndEventChannel.SendEvent(this, new LiquidationEndEventArgs
            {
                OperationId = operationId,
                CreationTime = _dateService.Now(),
                AccountId = executionInfo.Data.AccountId,
                LiquidatedPositionIds = executionInfo.Data.LiquidatedPositionIds,
                FailReason = reason,
            });
            
            await _log.WriteInfoAsync(nameof(LiquidationFailureExecutor),
                nameof(ExecuteAsync),
                new {account, operationId, reason}.ToJson(),
                $"Successfully published {nameof(LiquidationEndEventArgs)} event");
        }
    }
}