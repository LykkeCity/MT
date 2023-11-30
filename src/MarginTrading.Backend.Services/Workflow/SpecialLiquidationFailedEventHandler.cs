// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Common.Chaos;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Helpers;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Workflow
{
    public static class SpecialLiquidationCommandSenderExtensions
    {
        public static void SendResume(this ICommandSender sender,
            IOperationExecutionInfo<SpecialLiquidationOperationData> executionInfo,
            DateTime timestamp)
        {
            sender.SendCommand(new ResumeLiquidationInternalCommand
            {
                OperationId = executionInfo.Data.CausationOperationId,
                CreationTime = timestamp,
                Comment = $"Resume after special liquidation {executionInfo.Id} failed.",
                IsCausedBySpecialLiquidation = true,
                CausationOperationId = executionInfo.Id
            }, "TradingEngine");
        }
    }
    
    /// <summary>
    /// Event handler implementation for <see cref="SpecialLiquidationFailedEvent"/>
    /// </summary>
    public class SpecialLiquidationFailedEventHandler : 
        ISagaEventHandler<SpecialLiquidationFailedEvent>,
        ISpecialLiquidationSagaEventHandler
    {
        private readonly IDateService _dateService;
        private readonly IChaosKitty _chaosKitty;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly OrdersCache _ordersCache;
        private readonly LiquidationHelper _liquidationHelper;
        private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly IRfqPauseService _rfqPauseService;

        public SpecialLiquidationFailedEventHandler(IOperationExecutionInfoRepository operationExecutionInfoRepository,
            OrdersCache ordersCache,
            LiquidationHelper liquidationHelper,
            CqrsContextNamesSettings cqrsContextNamesSettings,
            IRfqPauseService rfqPauseService,
            IDateService dateService,
            IChaosKitty chaosKitty,
            MarginTradingSettings marginTradingSettings)
        {
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _ordersCache = ordersCache;
            _liquidationHelper = liquidationHelper;
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
            _rfqPauseService = rfqPauseService;
            _dateService = dateService;
            _chaosKitty = chaosKitty;
            _marginTradingSettings = marginTradingSettings;
        }

        private Task<IOperationExecutionInfo<SpecialLiquidationOperationData>> GetExecutionInfo(string operationId) =>
            _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                // todo: remove this dependency on SpecialLiquidationSaga class
                operationName: SpecialLiquidationSaga.Name,
                operationId);

        public async Task Handle(SpecialLiquidationFailedEvent @event, ICommandSender sender)
        {
            var eInfo = await GetExecutionInfo(@event.OperationId);
            
            if (eInfo?.Data == null)
                return;
            
            var isDiscontinued = await _liquidationHelper.FailIfInstrumentDiscontinued(eInfo, sender);
            if (isDiscontinued) return;
            
            if (!eInfo.Data.RequestedFromCorporateActions)
            {
                var positions = _ordersCache.Positions
                    .GetPositionsByAccountIds(eInfo.Data.AccountId)
                    .Where(p => eInfo.Data.PositionIds.Contains(p.Id))
                    .ToArray();
            
                if (_liquidationHelper.CheckIfNetVolumeCanBeLiquidated(eInfo.Data.Instrument, positions, out _))
                {
                    // there is liquidity so we can cancel the special liquidation flow.
                    sender.SendCommand(new CancelSpecialLiquidationCommand
                    {
                        OperationId = @event.OperationId,
                        Reason = "Liquidity is enough to close positions within regular flow"
                    }, _cqrsContextNamesSettings.TradingEngine);
                    return;
                }
            }
            
            if (SpecialLiquidationSaga.PriceRequestRetryRequired(
                    eInfo.Data.RequestedFromCorporateActions,
                    _marginTradingSettings.SpecialLiquidation) &&
                @event.CanRetryPriceRequest)
            {
                var pauseAcknowledged = await _rfqPauseService.AcknowledgeAsync(eInfo.Id);
                if (pauseAcknowledged) return;
            
                if (eInfo.SwitchToState(SpecialLiquidationOperationState.PriceRequested))
                {
                    await _liquidationHelper.InternalRetryPriceRequest(@event.CreationTime, sender, eInfo,
                        _marginTradingSettings.SpecialLiquidation.PriceRequestRetryTimeout.Value);
                
                    await _operationExecutionInfoRepository.Save(eInfo);
                
                    return;
                }
            }
            
            if (eInfo.SwitchToState(SpecialLiquidationOperationState.Failed))
            {
                if (!string.IsNullOrEmpty(eInfo.Data.CausationOperationId))
                {
                    sender.SendResume(eInfo, _dateService.Now());
                }
                
                _chaosKitty.Meow(@event.OperationId);
            
                await _operationExecutionInfoRepository.Save(eInfo);
            }
        }
    }
}