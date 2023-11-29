// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Cqrs;
using MarginTrading.Backend.Contracts.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Common.Services;
using MoreLinq;

namespace MarginTrading.Backend.Services.Helpers
{
    public class LiquidationHelper
    {
        private readonly IMatchingEngineRouter _matchingEngineRouter;
        private readonly IDateService _dateService;
        private readonly ICqrsSender _cqrsSender;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly CqrsContextNamesSettings _cqrsContextNamesSettings;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly IOperationExecutionInfoRepository _operationExecutionInfoRepository;
        private readonly IRfqService _specialLiquidationService;


        public LiquidationHelper(IMatchingEngineRouter matchingEngineRouter,
            IDateService dateService,
            ICqrsSender cqrsSender,
            OrdersCache ordersCache, 
            IAssetPairDayOffService assetPairDayOffService,
            IAssetPairsCache assetPairsCache,
            CqrsContextNamesSettings cqrsContextNamesSettings,
            IOperationExecutionInfoRepository operationExecutionInfoRepository,
            IRfqService specialLiquidationService,
            MarginTradingSettings marginTradingSettings)
        {
            _matchingEngineRouter = matchingEngineRouter;
            _dateService = dateService;
            _cqrsSender = cqrsSender;
            _ordersCache = ordersCache;
            _assetPairDayOffService = assetPairDayOffService;
            _assetPairsCache = assetPairsCache;
            _cqrsContextNamesSettings = cqrsContextNamesSettings;
            _operationExecutionInfoRepository = operationExecutionInfoRepository;
            _specialLiquidationService = specialLiquidationService;
            _marginTradingSettings = marginTradingSettings;
        }

        public bool CheckIfNetVolumeCanBeLiquidated(string assetPairId, Position[] positions,
            out string details)
        {
            var positionsWithDifferentAssetPair = positions.Where(p => p.AssetPairId != assetPairId).ToList();

            if (positionsWithDifferentAssetPair.Count > 0)
            {
                var positionsInfo = positionsWithDifferentAssetPair.Select(p => (p.Id, p.AssetPairId)).ToJson();

                throw new InvalidOperationException(
                    $"All positions should have the same asset pair {assetPairId}, but there are positions with different: {positionsInfo}");
            }

            foreach (var positionsGroup in positions.GroupBy(p => p.Direction))
            {
                var netPositionVolume = positionsGroup.Sum(p => p.Volume);

                //TODO: discuss and handle situation with different MEs for different positions
                //at the current moment all positions has the same asset pair
                //and every asset pair can be processed only by one ME
                var anyPosition = positionsGroup.First();
                var me = _matchingEngineRouter.GetMatchingEngineForClose(anyPosition.OpenMatchingEngineId);
                //the same for externalProvider.. 
                var externalProvider = anyPosition.ExternalProviderId;

                if (me.GetPriceForClose(assetPairId, netPositionVolume, externalProvider) == null)
                {
                    details =
                        $"Not enough depth of orderbook. Asset id {assetPairId}, net volume {netPositionVolume}, external provider {externalProvider}, matching engine {anyPosition.OpenMatchingEngineId}.";
                    return false;
                }
            }

            details = string.Empty;
            return true;
        }
        
        public Dictionary<string, (PositionCloseResult, Order)> StartLiquidation(string accountId,
            OriginatorType originator, string additionalInfo, string operationId)
        {
            var result = new Dictionary<string, (PositionCloseResult, Order)>();

            var command = new StartLiquidationInternalCommand
            {
                OperationId = operationId,
                CreationTime = _dateService.Now(),
                AccountId = accountId,
                LiquidationType = LiquidationType.Forced,
                OriginatorType = originator,
                AdditionalInfo = additionalInfo
            };

            _cqrsSender.SendCommandToSelf(command);

            var positions = _ordersCache.Positions.GetPositionsByAccountIds(accountId);
            
            var openPositions = new List<Position>();

            foreach (var position in positions)
            {
                switch (position.Status)
                {
                    case PositionStatus.Active:
                        openPositions.Add(position);
                        break;
                    case PositionStatus.Closing:
                        result.Add(position.Id, (PositionCloseResult.ClosingIsInProgress, null));
                        break;
                    case PositionStatus.Closed:
                        result.Add(position.Id, (PositionCloseResult.Closed, null));
                        break;
                    default:
                        throw new PositionValidationException(
                            $"Position [{position.Id}] status [{position.Status}] is not expected for special liquidation",
                            PositionValidationError.InvalidStatusWhenRunSpecialLiquidation);
                }
            }

            foreach (var group in openPositions.GroupBy(p => p.AssetPairId))
            {
                // if asset pair is not available for trading, we will not try to close these positions
                if (_assetPairDayOffService.IsAssetTradingDisabled(group.Key))
                    continue;
                
                var positionGroup = group.ToArray();
                
                // if the net volume can be liquidated, we assume that positions will be closed without special liquidation
                if (CheckIfNetVolumeCanBeLiquidated(group.Key, positionGroup, out _))
                {
                    positionGroup.ForEach(p => result.Add(p.Id, (PositionCloseResult.Closed, null)));
                }
                else
                {
                    positionGroup.ForEach(p => result.Add(p.Id, (PositionCloseResult.ClosingStarted, null)));
                }
            }

            return result;
        }
        
        public async Task<bool> FailIfInstrumentDiscontinued(IOperationExecutionInfo<SpecialLiquidationOperationData> executionInfo, ICommandSender sender)
        {
            var isDiscontinued = _assetPairsCache.GetAssetPairById(executionInfo.Data.Instrument).IsDiscontinued;
            
            if (isDiscontinued)
            {
                if (executionInfo.Data.SwitchState(SpecialLiquidationOperationState.PriceRequested,
                        SpecialLiquidationOperationState.OnTheWayToFail))
                {
                    sender.SendCommand(new FailSpecialLiquidationInternalCommand
                    {
                        OperationId = executionInfo.Id,
                        CreationTime = _dateService.Now(),
                        Reason = "Instrument discontinuation",
                            
                    }, _cqrsContextNamesSettings.TradingEngine);
                
                    await _operationExecutionInfoRepository.Save(executionInfo);
                }

                return true;
            }

            return false;
        }
        
        public async Task InternalRetryPriceRequest(DateTime eventCreationTime, 
            ICommandSender sender,
            IOperationExecutionInfo<SpecialLiquidationOperationData> executionInfo,
            TimeSpan retryTimeout)
        {
            // fix the intention to make another price request to not let the parallel 
            // ongoing GetPriceForSpecialLiquidationTimeoutInternalCommand execution
            // break (fail) the flow
            executionInfo.Data.NextRequestNumber();
            await _operationExecutionInfoRepository.Save(executionInfo);
            
            var shouldRetryAfter = eventCreationTime.Add(retryTimeout);

            var timeLeftBeforeRetry = shouldRetryAfter - _dateService.Now();

            if (timeLeftBeforeRetry > TimeSpan.Zero)
            {
                await Task.Delay(timeLeftBeforeRetry);
            }

            RequestPrice(sender, executionInfo);
        }
        
        public void RequestPrice(ICommandSender sender, IOperationExecutionInfo<SpecialLiquidationOperationData> 
            executionInfo)
        {
            //hack, requested by the bank
            var positionsVolume = executionInfo.Data.Volume != 0 ? executionInfo.Data.Volume : 1;
            
            var command = new GetPriceForSpecialLiquidationCommand
            {
                OperationId = executionInfo.Id,
                CreationTime = _dateService.Now(),
                Instrument = executionInfo.Data.Instrument,
                Volume = positionsVolume,
                RequestNumber = executionInfo.Data.RequestNumber,
                RequestedFromCorporateActions = executionInfo.Data.RequestedFromCorporateActions
            };
            
            if (_marginTradingSettings.ExchangeConnector == ExchangeConnectorType.RealExchangeConnector)
            {
                //send it to the Gavel
                sender.SendCommand(command, _cqrsContextNamesSettings.Gavel);
            }
            else
            {
                _specialLiquidationService.SavePriceRequestForSpecialLiquidation(command);
            }
            
            //special command is sent instantly for timeout control.. it is retried until timeout occurs
            sender.SendCommand(new GetPriceForSpecialLiquidationTimeoutInternalCommand
            {
                OperationId = executionInfo.Id,
                CreationTime = _dateService.Now(),
                TimeoutSeconds = _marginTradingSettings.SpecialLiquidation.PriceRequestTimeoutSec,
                RequestNumber = executionInfo.Data.RequestNumber
            }, _cqrsContextNamesSettings.TradingEngine);
        }

        public static string GetComment(LiquidationType liquidationType)
        {
            var comment = liquidationType switch
            {
                LiquidationType.Mco => "MCO liquidation",
                LiquidationType.Normal => "Liquidation",
                LiquidationType.Forced => "Close positions group",
                _ => string.Empty
            };

            return comment;
        }
    }
}