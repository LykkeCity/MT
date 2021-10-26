// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Common.Extensions;

namespace MarginTrading.Backend.Services.Infrastructure
{
    /// <inheritdoc/> 
    public class SnapshotValidationService : ISnapshotValidationService
    {
        private static readonly OrderStatus[] OrderTerminalStatuses =
            { OrderStatus.Canceled, OrderStatus.Rejected, OrderStatus.Executed, OrderStatus.Expired };

        private static readonly PositionHistoryType PositionTerminalStatus = PositionHistoryType.Close;

        private readonly ITradingEngineSnapshotsRepository _tradingEngineSnapshotsRepository;
        private readonly IOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IPositionsHistoryRepository _positionsHistoryRepository;
        private readonly IOrderReader _orderCache;
        private readonly ILog _log;

        public SnapshotValidationService(
            ITradingEngineSnapshotsRepository tradingEngineSnapshotsRepository,
            IOrdersHistoryRepository ordersHistoryRepository,
            IPositionsHistoryRepository positionsHistoryRepository,
            IOrderReader orderCache,
            ILog log)
        {
            _tradingEngineSnapshotsRepository = tradingEngineSnapshotsRepository;
            _ordersHistoryRepository = ordersHistoryRepository;
            _positionsHistoryRepository = positionsHistoryRepository;
            _orderCache = orderCache;
            _log = log;
        }

        /// <inheritdoc/> 
        public async Task<SnapshotValidationResult> ValidateCurrentStateAsync()
        {
            await _log.WriteInfoAsync(nameof(SnapshotValidationService), nameof(ValidateCurrentStateAsync),
                $"Snapshot validation started: {DateTime.UtcNow}");
            var currentOrders = _orderCache.GetAllOrders();
            var currentPositions = _orderCache.GetPositions();

            var tradingEngineSnapshot = await _tradingEngineSnapshotsRepository.GetLastAsync();
            await _log.WriteInfoAsync(nameof(SnapshotValidationService), nameof(ValidateCurrentStateAsync),
                $"Last snapshot correlationId {tradingEngineSnapshot.CorrelationId}, tradingDay {tradingEngineSnapshot.TradingDay}, timestamp {tradingEngineSnapshot.Timestamp}");
            
            var lastOrders = GetOrders(tradingEngineSnapshot);
            var lastPositions = GetPositions(tradingEngineSnapshot);

            var ordersHistory = await _ordersHistoryRepository.GetLastSnapshot(tradingEngineSnapshot.Timestamp);
            var positionsHistory = await _positionsHistoryRepository.GetLastSnapshot(tradingEngineSnapshot.Timestamp);

            var restoredOrders = RestoreOrdersCurrentStateFromHistory(lastOrders, ordersHistory);
            var restoredPositions = RestorePositionsCurrentStateFromHistory(lastPositions, positionsHistory);

            var ordersValidationResult = CompareOrders(currentOrders, restoredOrders);
            var positionsValidationResult = ComparePositions(currentPositions, restoredPositions);

            if (ordersValidationResult.IsValid)
            {
                await _log.WriteInfoAsync(nameof(SnapshotValidationService), nameof(ValidateCurrentStateAsync),
                    $"Orders validation result is valid");
            }
            else
            {
                await _log.WriteWarningAsync(nameof(SnapshotValidationService), nameof(ValidateCurrentStateAsync),
                    $"Orders validation result is NOT valid. Extra: {ordersValidationResult.Extra.Count}, missed: {ordersValidationResult.Missed.Count}, inconsistent: {ordersValidationResult.Inconsistent.Count}");
            }

            if (positionsValidationResult.IsValid)
            {
                await _log.WriteInfoAsync(nameof(SnapshotValidationService), nameof(ValidateCurrentStateAsync),
                    $"Positions validation result is valid");
            }
            else
            {
                await _log.WriteWarningAsync(nameof(SnapshotValidationService), nameof(ValidateCurrentStateAsync),
                    $"Positions validation result is NOT valid. Extra: {positionsValidationResult.Extra.Count}, missed: {positionsValidationResult.Missed.Count}, inconsistent: {positionsValidationResult.Inconsistent.Count}");
            }

            return new SnapshotValidationResult
            {
                Orders = ordersValidationResult,
                Positions = positionsValidationResult,
                PreviousSnapshotCorrelationId = tradingEngineSnapshot.CorrelationId
            };
        }

        private static IReadOnlyList<OrderInfo> RestoreOrdersCurrentStateFromHistory(
            IEnumerable<OrderContract> lastOrders, IEnumerable<IOrderHistory> ordersHistory)
        {
            var lastOrdersMap = lastOrders.ToDictionary(o => o.Id, o => o);
            var ordersHistoryMap = ordersHistory.ToDictionary(o => o.Id, o => o);

            var unchangedOrders = lastOrdersMap.Keys
                .Except(ordersHistoryMap.Keys)
                .Select(orderId => lastOrdersMap[orderId])
                .Select(order => new OrderInfo(order.Id, order.Volume ?? 0, order.ExpectedOpenPrice,
                    order.Status.ToType<OrderStatus>(), order.Type.ToType<OrderType>()));

            var newOrders = ordersHistoryMap.Keys
                .Except(lastOrdersMap.Keys)
                .Select(orderId => ordersHistoryMap[orderId])
                .Where(order => !OrderTerminalStatuses.Contains(order.Status))
                .Select(Map);

            var changedOrders = ordersHistoryMap.Keys
                .Intersect(lastOrdersMap.Keys)
                .Select(orderId => ordersHistoryMap[orderId])
                .Where(order => !OrderTerminalStatuses.Contains(order.Status))
                .Select(Map);

            return unchangedOrders
                .Union(newOrders)
                .Union(changedOrders)
                .ToList();
        }

        private static IReadOnlyList<PositionInfo> RestorePositionsCurrentStateFromHistory(
            IEnumerable<OpenPositionContract> lastPositions, IEnumerable<IPositionHistory> positionsHistory)
        {
            var lastPositionsMap = lastPositions.ToDictionary(o => o.Id, o => o);
            var positionsHistoryMap = positionsHistory.ToDictionary(o => o.Id, o => o);

            var unchangedPositions = lastPositionsMap.Keys
                .Except(positionsHistoryMap.Keys)
                .Select(positionId => lastPositionsMap[positionId])
                .Select(position => new PositionInfo(position.Id, position.CurrentVolume));

            var newPositions = positionsHistoryMap.Keys
                .Except(lastPositionsMap.Keys)
                .Select(positionId => positionsHistoryMap[positionId])
                .Where(position => PositionTerminalStatus != position.HistoryType)
                .Select(position => new PositionInfo(position.Id, position.Volume));

            var changedPositions = positionsHistoryMap.Keys
                .Intersect(lastPositionsMap.Keys)
                .Select(positionId => positionsHistoryMap[positionId])
                .Where(position => PositionTerminalStatus != position.HistoryType)
                .Select(position => new PositionInfo(position.Id, position.Volume));

            return unchangedPositions
                .Union(newPositions)
                .Union(changedPositions)
                .ToList();
        }

        private static ValidationResult<OrderInfo> CompareOrders(ImmutableArray<Order> currentOrders,
            IEnumerable<OrderInfo> restoredOrders)
        {
            var currentOrdersMap = currentOrders.ToDictionary(o => o.Id, o => o);
            var restoredOrdersMap = restoredOrders.ToDictionary(o => o.Id, o => o);

            var extraOrders = currentOrdersMap.Keys
                .Except(restoredOrdersMap.Keys)
                .Select(orderId => currentOrdersMap[orderId])
                .Select(order => new OrderInfo(order.Id, order.Volume, order.Price, order.Status, order.OrderType));

            var missedOrders = restoredOrdersMap.Keys
                .Except(currentOrdersMap.Keys)
                .Select(orderId => restoredOrdersMap[orderId]);

            var inconsistentOrders = restoredOrdersMap.Keys
                .Intersect(currentOrdersMap.Keys)
                .Select(orderId => new ValidationPair<OrderInfo>()
                {
                    Restored = restoredOrdersMap[orderId],
                    Current = Map(currentOrdersMap[orderId])
                })
                .Where(pair => pair.Restored.Volume != pair.Current.Volume 
                               ||
                               (pair.Restored.ExpectedOpenPrice != pair.Current.ExpectedOpenPrice 
                                && pair.Current.Type != OrderType.TrailingStop) 
                               ||
                               (pair.Restored.Status != pair.Current.Status));

            return new ValidationResult<OrderInfo>
            {
                Extra = extraOrders.ToList(),
                Missed = missedOrders.ToList(),
                Inconsistent = inconsistentOrders.ToList()
            };
        }

        private static ValidationResult<PositionInfo> ComparePositions(ImmutableArray<Position> currentPositions,
            IEnumerable<PositionInfo> restoredPositions)
        {
            var currentPositionsMap = currentPositions.ToDictionary(o => o.Id, o => o);
            var restoredPositionsMap = restoredPositions.ToDictionary(o => o.Id, o => o);

            var extraPositions = currentPositionsMap.Keys
                .Except(restoredPositionsMap.Keys)
                .Select(positionId => currentPositionsMap[positionId])
                .Select(position => new PositionInfo(position.Id, position.Volume));

            var missedPositions = restoredPositionsMap.Keys
                .Except(currentPositionsMap.Keys)
                .Select(positionId => restoredPositionsMap[positionId]);

            var inconsistentPositions = restoredPositionsMap.Keys
                .Intersect(currentPositionsMap.Keys)
                .Select(positionId => new ValidationPair<PositionInfo>()
                {
                    Restored = restoredPositionsMap[positionId],
                    Current = Map(currentPositionsMap[positionId])
                })
                .Where(pair => pair.Restored.Volume != pair.Current.Volume);

            return new ValidationResult<PositionInfo>
            {
                Extra = extraPositions.ToList(),
                Missed = missedPositions.ToList(),
                Inconsistent = inconsistentPositions.ToList()
            };
        }

        private static IReadOnlyList<OrderContract> GetOrders(TradingEngineSnapshot tradingEngineSnapshot)
            => !string.IsNullOrEmpty(tradingEngineSnapshot.OrdersJson)
                ? tradingEngineSnapshot.OrdersJson.DeserializeJson<List<OrderContract>>()
                : new List<OrderContract>();

        private static IReadOnlyList<OpenPositionContract> GetPositions(TradingEngineSnapshot tradingEngineSnapshot)
            => !string.IsNullOrEmpty(tradingEngineSnapshot.PositionsJson)
                ? tradingEngineSnapshot.PositionsJson.DeserializeJson<List<OpenPositionContract>>()
                : new List<OpenPositionContract>();

        private static OrderInfo Map(Order order)
        {
            return new OrderInfo(order.Id, order.Volume, order.Price, order.Status, order.OrderType);
        }
        
        private static OrderInfo Map(IOrderHistory order)
        {
            var status = order.Status == OrderStatus.Placed ? OrderStatus.Inactive : order.Status;
            return new OrderInfo(order.Id, order.Volume, order.ExpectedOpenPrice, status, order.Type);
        }

        private static PositionInfo Map(Position position)
        {
            return new PositionInfo(position.Id, position.Volume);
        }
    }
}