// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
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


        public LiquidationHelper(IMatchingEngineRouter matchingEngineRouter,
            IDateService dateService,
            ICqrsSender cqrsSender,
            OrdersCache ordersCache, 
            IAssetPairDayOffService assetPairDayOffService)
        {
            _matchingEngineRouter = matchingEngineRouter;
            _dateService = dateService;
            _cqrsSender = cqrsSender;
            _ordersCache = ordersCache;
            _assetPairDayOffService = assetPairDayOffService;
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
                    details = $"Not enough depth of orderbook. Net volume : {netPositionVolume}.";
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
                        throw new InvalidOperationException($"Position state {position.Status.ToString()} is not handled");
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