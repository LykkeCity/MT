// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Workflow
{
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
        private readonly ILogger<SpecialLiquidationFailedEventHandler> _logger;

        public SpecialLiquidationFailedEventHandler(ILogger<SpecialLiquidationFailedEventHandler> logger,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            OrdersCache ordersCache,
            LiquidationHelper liquidationHelper,
            CqrsContextNamesSettings cqrsContextNamesSettings,
            IRfqPauseService rfqPauseService,
            IDateService dateService,
            IChaosKitty chaosKitty,
            MarginTradingSettings marginTradingSettings)
        {
            _logger = logger;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _ordersCache = ordersCache;
            _liquidationHelper = liquidationHelper;
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
            _rfqPauseService = rfqPauseService;
            _dateService = dateService;
            _chaosKitty = chaosKitty;
            _marginTradingSettings = marginTradingSettings;
        }

        public async Task Handle(SpecialLiquidationFailedEvent @event, ICommandSender sender)
        {
            var executionInfo = await _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                // todo: remove this dependency on SpecialLiquidationSaga class
                operationName: SpecialLiquidationSaga.Name,
                id: @event.OperationId);
            
            if (executionInfo?.Data == null)
                return;
            
            if (@event.CanRetryPriceRequest)
            {
                if (!executionInfo.Data.RequestedFromCorporateActions)
                {
                    var positions = _ordersCache.Positions
                        .GetPositionsByAccountIds(executionInfo.Data.AccountId)
                        .Where(p => executionInfo.Data.PositionIds.Contains(p.Id))
                        .ToArray();

                    if (_liquidationHelper.CheckIfNetVolumeCanBeLiquidated(executionInfo.Data.Instrument, positions, out _))
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

                if (SpecialLiquidationSaga.PriceRequestRetryRequired(executionInfo.Data.RequestedFromCorporateActions, 
                        _marginTradingSettings.SpecialLiquidation))
                {
                    var isDiscontinued = await _liquidationHelper.FailIfInstrumentDiscontinued(executionInfo, sender);
                    if (isDiscontinued) return;
                    
                    var pauseAcknowledged = await _rfqPauseService.AcknowledgeAsync(executionInfo.Id);
                    if (pauseAcknowledged) return;
                    
                    if (executionInfo.Data.SwitchState(executionInfo.Data.State,
                            SpecialLiquidationOperationState.PriceRequested))
                    {
                        await _liquidationHelper.InternalRetryPriceRequest(@event.CreationTime, sender, executionInfo,
                            _marginTradingSettings.SpecialLiquidation.PriceRequestRetryTimeout.Value);
                        
                        return;
                    }
                }
            }

            if (executionInfo.Data.SwitchState(executionInfo.Data.State,//from any state
                SpecialLiquidationOperationState.Failed))
            {
                if (!string.IsNullOrEmpty(executionInfo.Data.CausationOperationId))
                {
                    sender.SendCommand(new ResumeLiquidationInternalCommand
                    {
                        OperationId = executionInfo.Data.CausationOperationId,
                        CreationTime = _dateService.Now(),
                        Comment = $"Resume after special liquidation {executionInfo.Id} failed. Reason: {@event.Reason}",
                        IsCausedBySpecialLiquidation = true,
                        CausationOperationId = executionInfo.Id
                    }, _cqrsContextNamesSettings.TradingEngine);
                }
                
                _chaosKitty.Meow(@event.OperationId);

                await _operationExecutionInfoRepository.Save(executionInfo);
            }
        }
    }
}