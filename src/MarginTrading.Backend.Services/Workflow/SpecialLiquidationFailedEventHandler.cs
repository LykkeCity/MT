// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Events;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Helpers;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation;
using MarginTrading.Common.Services;
using ExecutionInfo = MarginTrading.Backend.Core.IOperationExecutionInfo<MarginTrading.Backend.Core.SpecialLiquidationOperationData>;

namespace MarginTrading.Backend.Services.Workflow
{
    /// <summary>
    /// Event handler implementation for <see cref="SpecialLiquidationFailedEvent"/>
    /// </summary>
    public class SpecialLiquidationFailedEventHandler : 
        ISagaEventHandler<SpecialLiquidationFailedEvent>,
        ISpecialLiquidationSagaEventHandler
    {
        /// <summary>
        /// The possible verdicts of the event handler what to do next
        /// </summary>
        internal enum NextAction
        {
            /// <summary>
            ///  Finish the special liquidation flow 
            /// </summary>
            Complete,
            
            /// <summary>
            /// Cancel the special liquidation flow
            /// </summary>
            Cancel,
            
            /// <summary>
            /// Pause the special liquidation flow
            /// </summary>
            Pause,
            
            /// <summary>
            /// Continue with price request retry
            /// </summary>
            RetryPriceRequest,
            
            /// <summary>
            /// Resume the initial flow caused the special liquidation
            /// </summary>
            ResumeInitialFlow
        }
        
        /// <summary>
        /// Map of the possible next states of the special liquidation flow.
        /// Null means that the state should not be changed. 
        /// </summary>
        private static readonly Dictionary<NextAction, SpecialLiquidationOperationState?> NextStateMap =
            new Dictionary<NextAction, SpecialLiquidationOperationState?>
            {
                { NextAction.Complete, SpecialLiquidationOperationState.Failed },
                { NextAction.Cancel, SpecialLiquidationOperationState.Cancelled },
                { NextAction.Pause, null },
                { NextAction.RetryPriceRequest, SpecialLiquidationOperationState.PriceRequested },
                { NextAction.ResumeInitialFlow, SpecialLiquidationOperationState.Failed },
            };
        
        private readonly IDateService _dateService;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly OrdersCache _ordersCache;
        private readonly LiquidationHelper _liquidationHelper;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly IRfqPauseService _rfqPauseService;
        private readonly IAssetPairsCache _assetPairsCache;

        public SpecialLiquidationFailedEventHandler(IOperationExecutionInfoRepository operationExecutionInfoRepository,
            OrdersCache ordersCache,
            LiquidationHelper liquidationHelper,
            IRfqPauseService rfqPauseService,
            IDateService dateService,
            MarginTradingSettings marginTradingSettings,
            IAssetPairsCache assetPairsCache)
        {
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _ordersCache = ordersCache;
            _liquidationHelper = liquidationHelper;
            _rfqPauseService = rfqPauseService;
            _dateService = dateService;
            _marginTradingSettings = marginTradingSettings;
            _assetPairsCache = assetPairsCache;
        }
        
        public async Task Handle(SpecialLiquidationFailedEvent @event, ICommandSender sender)
        {
            var executionInfo = await GetExecutionInfo(@event.OperationId);

            var instrument = _assetPairsCache.GetAssetPairById(executionInfo!.Data.Instrument);

            var nextAction = await DetermineNextAction(
                executionInfo: executionInfo,
                instrumentDiscontinued: instrument.IsDiscontinued,
                liquidityIsEnough: GetLiquidityIsEnough,
                canRetryPriceRequest: @event.CanRetryPriceRequest,
                pauseIsAcknowledged: _rfqPauseService.AcknowledgeAsync,
                configuration: _marginTradingSettings.SpecialLiquidation);

            var nextState = GetNextState(nextAction);
            if (!await TrySaveState(executionInfo, nextState))
                return;
            
            switch (nextAction)
            {
                case NextAction.Cancel:
                    sender.SendCancellation(@event.OperationId);
                    break;
                case NextAction.ResumeInitialFlow:
                    sender.SendResumeLiquidation(executionInfo.Data.CausationOperationId, 
                        executionInfo.Id, 
                        _dateService.Now());
                    break;
                case NextAction.RetryPriceRequest:
                    await _liquidationHelper.InternalRetryPriceRequest(@event.CreationTime, 
                        sender, 
                        executionInfo,
                        _marginTradingSettings.SpecialLiquidation.PriceRequestRetryTimeout!.Value);
                    break;
                default:
                    return;
            }
        }
        
        public async Task<bool> CanHandle(SpecialLiquidationFailedEvent @event)
        {
            var executionInfo = await GetExecutionInfo(@event.OperationId);

            return executionInfo?.Data != null;
        }

        /// <summary>
        /// Pure business logic to determine what to be the next step of the special liquidation flow
        /// </summary>
        /// <param name="executionInfo">Current special liquidation execution info</param>
        /// <param name="instrumentDiscontinued">Current state of discontinuation of the instrument</param>
        /// <param name="liquidityIsEnough">Function to check if there is enough liquidity to close the positions</param>
        /// <param name="canRetryPriceRequest">Whether the price request can be retried</param>
        /// <param name="pauseIsAcknowledged">Function to check if the pause was requested and acknowledged</param>
        /// <param name="configuration">Special liquidation settings</param>
        /// <returns></returns>
        [Pure]
        internal static async Task<NextAction> DetermineNextAction(
            ExecutionInfo executionInfo, 
            bool instrumentDiscontinued,
            Func<ExecutionInfo, bool> liquidityIsEnough,
            bool canRetryPriceRequest,
            Func<string, Task<bool>> pauseIsAcknowledged,
            SpecialLiquidationSettings configuration)
        {
            if (instrumentDiscontinued)
                return NextAction.Complete;

            var caInitiated = executionInfo.Data.RequestedFromCorporateActions;
            if (!caInitiated && liquidityIsEnough(executionInfo))
                return NextAction.Cancel;

            var retryRequired = SpecialLiquidationSaga.PriceRequestRetryRequired(caInitiated, configuration);
            if (retryRequired && canRetryPriceRequest)
            {
                if (await pauseIsAcknowledged(executionInfo.Id))
                    return NextAction.Pause;

                return NextAction.RetryPriceRequest;
            }
            
            var hasCausingLiquidation = !string.IsNullOrEmpty(executionInfo.Data.CausationOperationId);
            if (hasCausingLiquidation)
                return NextAction.ResumeInitialFlow;

            return NextAction.Complete;
        }

        private static SpecialLiquidationOperationState? GetNextState(NextAction nextAction) =>
            NextStateMap.TryGetValue(nextAction, out var state) ? state : null;

        private bool GetLiquidityIsEnough(ExecutionInfo executionInfo)
        {
            var positions = _ordersCache.Positions
                .GetPositionsByAccountIds(executionInfo.Data.AccountId)
                .Where(p => executionInfo.Data.PositionIds.Contains(p.Id))
                .ToArray();

            return _liquidationHelper.CheckIfNetVolumeCanBeLiquidated(executionInfo.Data.Instrument, positions, out _);
        }

        private async Task<bool> TrySaveState(ExecutionInfo executionInfo, SpecialLiquidationOperationState? state)
        {
            if (!state.HasValue)
                return false;
            
            if (executionInfo.SwitchToState(state.Value))
            {
                await _operationExecutionInfoRepository.Save(executionInfo);
                return true;
            }

            return false;
        }

        private Task<IOperationExecutionInfo<SpecialLiquidationOperationData>> GetExecutionInfo(string operationId) =>
            _operationExecutionInfoRepository.GetAsync<SpecialLiquidationOperationData>(
                operationName: SpecialLiquidationSaga.Name,
                operationId);
    }
}