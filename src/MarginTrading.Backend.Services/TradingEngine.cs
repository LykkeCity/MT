// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common;
using Lykke.Snow.Common.Correlation;
using MarginTrading.Backend.Contracts.Activities;
using MarginTrading.Backend.Contracts.TradingSchedule;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Extensions;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Helpers;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.Workflow.Liquidation.Commands;
using MarginTrading.Backend.Services.Workflow.SpecialLiquidation.Commands;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services
{
    public sealed class TradingEngine : ITradingEngine,
        IEventConsumer<BestPriceChangeEventArgs>,
        IEventConsumer<FxBestPriceChangeEventArgs>
    {
        private readonly IEventChannel<MarginCallEventArgs> _marginCallEventChannel;
        private readonly IEventChannel<OrderPlacedEventArgs> _orderPlacedEventChannel;
        private readonly IEventChannel<OrderExecutedEventArgs> _orderExecutedEventChannel;
        private readonly IEventChannel<OrderCancelledEventArgs> _orderCancelledEventChannel;
        private readonly IEventChannel<OrderChangedEventArgs> _orderChangedEventChannel;
        private readonly IEventChannel<OrderExecutionStartedEventArgs> _orderExecutionStartedEvenChannel;
        private readonly IEventChannel<OrderActivatedEventArgs> _orderActivatedEventChannel;
        private readonly IEventChannel<OrderRejectedEventArgs> _orderRejectedEventChannel;

        private readonly IOrderValidator _orderValidator;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        private readonly IMatchingEngineRouter _meRouter;
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly ILog _log;
        private readonly IDateService _dateService;
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IIdentityGenerator _identityGenerator;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ICqrsSender _cqrsSender;
        private readonly IEventChannel<StopOutEventArgs> _stopOutEventChannel;
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly LiquidationHelper _liquidationHelper;
        private readonly CorrelationContextAccessor _correlationContextAccessor;

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _accountSemaphores =
            new ConcurrentDictionary<string, SemaphoreSlim>();

        public TradingEngine(
            IEventChannel<MarginCallEventArgs> marginCallEventChannel,
            IEventChannel<OrderPlacedEventArgs> orderPlacedEventChannel,
            IEventChannel<OrderExecutedEventArgs> orderClosedEventChannel,
            IEventChannel<OrderCancelledEventArgs> orderCancelledEventChannel,
            IEventChannel<OrderChangedEventArgs> orderChangedEventChannel,
            IEventChannel<OrderExecutionStartedEventArgs> orderExecutionStartedEventChannel,
            IEventChannel<OrderActivatedEventArgs> orderActivatedEventChannel,
            IEventChannel<OrderRejectedEventArgs> orderRejectedEventChannel,
            IOrderValidator orderValidator,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IMatchingEngineRouter meRouter,
            IThreadSwitcher threadSwitcher,
            IAssetPairDayOffService assetPairDayOffService,
            ILog log,
            IDateService dateService,
            ICfdCalculatorService cfdCalculatorService,
            IIdentityGenerator identityGenerator,
            IAssetPairsCache assetPairsCache,
            ICqrsSender cqrsSender,
            IEventChannel<StopOutEventArgs> stopOutEventChannel,
            IQuoteCacheService quoteCacheService,
            MarginTradingSettings marginTradingSettings,
            LiquidationHelper liquidationHelper,
            CorrelationContextAccessor correlationContextAccessor)
        {
            _marginCallEventChannel = marginCallEventChannel;
            _orderPlacedEventChannel = orderPlacedEventChannel;
            _orderExecutedEventChannel = orderClosedEventChannel;
            _orderCancelledEventChannel = orderCancelledEventChannel;
            _orderActivatedEventChannel = orderActivatedEventChannel;
            _orderExecutionStartedEvenChannel = orderExecutionStartedEventChannel;
            _orderChangedEventChannel = orderChangedEventChannel;
            _orderRejectedEventChannel = orderRejectedEventChannel;

            _orderValidator = orderValidator;
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _meRouter = meRouter;
            _threadSwitcher = threadSwitcher;
            _assetPairDayOffService = assetPairDayOffService;
            _log = log;
            _dateService = dateService;
            _cfdCalculatorService = cfdCalculatorService;
            _identityGenerator = identityGenerator;
            _assetPairsCache = assetPairsCache;
            _cqrsSender = cqrsSender;
            _stopOutEventChannel = stopOutEventChannel;
            _quoteCacheService = quoteCacheService;
            _marginTradingSettings = marginTradingSettings;
            _liquidationHelper = liquidationHelper;
            _correlationContextAccessor = correlationContextAccessor;
        }

        public async Task<Order> PlaceOrderAsync(Order order)
        {
            _orderPlacedEventChannel.SendEvent(this, new OrderPlacedEventArgs(order));

            try
            {
                if (order.OrderType != OrderType.Market)
                {
                    await PlacePendingOrder(order);
                    return order;
                }

                return await PlaceOrderByMarketPrice(order);
            }
            catch (OrderRejectionException ex)
            {
                RejectOrder(order, ex.RejectReason, ex.Message, ex.Comment);
                return order;
            }
            catch (Exception ex)
            {
                RejectOrder(order, OrderRejectReason.TechnicalError, ex.Message);
                _log.WriteError(nameof(TradingEngine), nameof(PlaceOrderAsync), ex);
                return order;
            }
        }

        private async Task<Order> PlaceOrderByMarketPrice(Order order)
        {
            try
            {
                var me = _meRouter.GetMatchingEngineForExecution(order);

                foreach (var positionId in order.PositionsToBeClosed)
                {
                    if (!_ordersCache.Positions.TryGetPositionById(positionId, out var position))
                    {
                        RejectOrder(order, OrderRejectReason.ParentPositionDoesNotExist, positionId);
                        return order;
                    }

                    if (position.Status != PositionStatus.Active)
                    {
                        RejectOrder(order, OrderRejectReason.ParentPositionIsNotActive, positionId);
                        return order;
                    }

                    position.StartClosing(_dateService.Now(), order.OrderType.GetCloseReason(), order.Originator, "");
                }

                return await ExecuteOrderByMatchingEngineAsync(order, me, true);
            }
            catch (Exception ex)
            {
                var reason = ex is QuoteNotFoundException
                    ? OrderRejectReason.NoLiquidity
                    : OrderRejectReason.TechnicalError;
                RejectOrder(order, reason, ex.Message);
                _log.WriteError(nameof(TradingEngine), nameof(PlaceOrderByMarketPrice), ex);
                return order;
            }
        }

        private async Task<Order> ExecutePendingOrder(Order order)
        {
            await PlaceOrderByMarketPrice(order);

            if (order.IsExecutionNotStarted)
            {
                foreach (var positionId in order.PositionsToBeClosed)
                {
                    if (_ordersCache.Positions.TryGetPositionById(positionId, out var position)
                        && position.Status == PositionStatus.Closing)
                    {
                        position.CancelClosing(_dateService.Now());
                    }
                }
            }

            return order;
        }

        private async Task PlacePendingOrder(Order order)
        {
            if (order.IsBasicPendingOrder() || !string.IsNullOrEmpty(order.ParentPositionId))
            {
                Position parentPosition = null;

                if (!string.IsNullOrEmpty(order.ParentPositionId))
                {
                    parentPosition = _ordersCache.Positions.GetPositionById(order.ParentPositionId);
                    parentPosition.AddRelatedOrder(order);
                }

                order.Activate(_dateService.Now(), false, parentPosition?.ClosePrice);
                _ordersCache.Active.Add(order);
                _orderActivatedEventChannel.SendEvent(this, new OrderActivatedEventArgs(order));
            }
            else if (!string.IsNullOrEmpty(order.ParentOrderId))
            {
                if (_ordersCache.TryGetOrderById(order.ParentOrderId, out var parentOrder))
                {
                    parentOrder.AddRelatedOrder(order);
                    order.MakeInactive(_dateService.Now());
                    _ordersCache.Inactive.Add(order);
                    return;
                }

                //may be it was market and now it is position
                if (_ordersCache.Positions.TryGetPositionById(order.ParentOrderId, out var parentPosition))
                {
                    parentPosition.AddRelatedOrder(order);
                    if (parentPosition.Volume != -order.Volume)
                    {
                        order.ChangeVolume(-parentPosition.Volume, _dateService.Now(), OriginatorType.System);
                    }

                    order.Activate(_dateService.Now(), true, parentPosition.ClosePrice);
                    _ordersCache.Active.Add(order);
                    _orderActivatedEventChannel.SendEvent(this, new OrderActivatedEventArgs(order));
                }
                else
                {
                    order.MakeInactive(_dateService.Now());
                    _ordersCache.Inactive.Add(order);
                    CancelPendingOrder(order.Id, order.AdditionalInfo, $"Parent order closed the position, so {order.OrderType.ToString()} order is cancelled");
                }
            }
            else
            {
                throw new OrderRejectionException(OrderRejectReason.InvalidParent, "Order parent is not valid");
            }

            await ExecutePendingOrderIfNeededAsync(order);
        }

        private async Task<Order> ExecuteOrderByMatchingEngineAsync(Order order, IMatchingEngineBase matchingEngine,
            bool checkStopout, OrderModality modality = OrderModality.Regular)
        {
            var semaphore = _accountSemaphores.GetOrAdd(order.AccountId, new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync();

            try
            {
                //just in case )
                if (CheckIfOrderIsExpired(order, _dateService.Now()))
                {
                    return order;
                }

                order.StartExecution(_dateService.Now(), matchingEngine.Id);

                _orderExecutionStartedEvenChannel.SendEvent(this, new OrderExecutionStartedEventArgs(order));

                ChangeOrderVolumeIfNeeded(order);

                var equivalentRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
                    order.AssetPairId, order.LegalEntity);
                var fxRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.AccountAssetId,
                    order.AssetPairId, order.LegalEntity);

                order.SetRates(equivalentRate, fxRate);

                var orderFulfillmentPlan = MatchOnExistingPositions(order);

                if (modality == OrderModality.Regular && order.Originator != OriginatorType.System)
                {
                    try
                    {
                        _orderValidator.PreTradeValidate(orderFulfillmentPlan, matchingEngine);
                    }
                    catch (OrderRejectionException ex)
                    {
                        RejectOrder(order, ex.RejectReason, ex.Message, ex.Comment);
                        return order;
                    }
                }

                MatchedOrderCollection matchedOrders;
                try
                {
                    matchedOrders = await matchingEngine.MatchOrderAsync(orderFulfillmentPlan, modality);
                }
                catch (OrderExecutionTechnicalException)
                {
                    RejectOrder(order, OrderRejectReason.TechnicalError, $"Unexpected reject (Order ID: {order.Id})");
                    return order;
                }

                if (!matchedOrders.Any())
                {
                    RejectOrder(order, OrderRejectReason.NoLiquidity, "No orders to match", "");
                    return order;
                }

                if (matchedOrders.SummaryVolume < Math.Abs(order.Volume))
                {
                    if (order.FillType == OrderFillType.FillOrKill)
                    {
                        RejectOrder(order, OrderRejectReason.NoLiquidity, "Not fully matched", "");
                        return order;
                    }
                    else
                    {
                        order.PartiallyExecute(_dateService.Now(), matchedOrders);
                        _ordersCache.InProgress.Add(order);
                        return order;
                    }
                }

                if (order.Status == OrderStatus.ExecutionStarted)
                {
                    var accuracy = _assetPairsCache.GetAssetPairByIdOrDefault(order.AssetPairId)?.Accuracy ??
                                   AssetPairsCache.DefaultAssetPairAccuracy;

                    order.Execute(_dateService.Now(), matchedOrders, accuracy);

                    _orderExecutedEventChannel.SendEvent(this, new OrderExecutedEventArgs(order));

                    if (checkStopout)
                    {
                        await CheckStopout(order);
                    }
                }

                return order;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private bool CheckIfOrderIsExpired(Order order, DateTime now)
        {
            if (order.OrderType != OrderType.Market &&
                order.Validity.HasValue &&
                now.Date > order.Validity.Value.Date)
            {
                order.Expire(now);
                _orderCancelledEventChannel.SendEvent(this,
                    new OrderCancelledEventArgs(order,
                        new OrderCancelledMetadata { Reason = OrderCancellationReasonContract.Expired }));
                return true;
            }

            return false;
        }

        private void ChangeOrderVolumeIfNeeded(Order order)
        {
            if (!order.PositionsToBeClosed.Any()) return;

            var netVolume = 0M;
            var rejectReason = default(OrderRejectReason?);
            foreach (var positionId in order.PositionsToBeClosed)
            {
                if (!_ordersCache.Positions.TryGetPositionById(positionId, out var position))
                {
                    rejectReason = OrderRejectReason.ParentPositionDoesNotExist;
                    continue;
                }

                if (position.Status != PositionStatus.Closing)
                {
                    rejectReason = OrderRejectReason.TechnicalError;
                    continue;
                }

                netVolume += position.Volume;
            }

            if (netVolume == 0M && rejectReason.HasValue)
            {
                order.Reject(rejectReason.Value,
                    rejectReason.Value == OrderRejectReason.ParentPositionDoesNotExist
                        ? "Related position does not exist"
                        : "Related position is not in closing state", "", _dateService.Now());
                _orderRejectedEventChannel.SendEvent(this, new OrderRejectedEventArgs(order));
                return;
            }

            // there is no any global lock of positions / orders, that's why it is possible to have concurrency 
            // in position close process
            // since orders, that have not empty PositionsToBeClosed should close positions and not open new ones
            // volume of executed order should be equal to position volume, but should have opposite sign
            if (order.Volume != -netVolume)
            {
                var metadata = new OrderChangedMetadata
                {
                    OldValue = order.Volume.ToString("F2"),
                    UpdatedProperty = OrderChangedProperty.Volume
                };
                order.ChangeVolume(-netVolume, _dateService.Now(), order.Originator);
                _orderChangedEventChannel.SendEvent(this, new OrderChangedEventArgs(order, metadata));
            }
        }

        private async Task CheckStopout(Order order)
        {
            var account = _accountsCacheService.Get(order.AccountId);
            var accountLevel = account.GetAccountLevel();

            if (accountLevel == AccountLevel.StopOut)
            {
                await CommitStopOut(account, null);
            }
            else if (accountLevel > AccountLevel.None)
            {
                _marginCallEventChannel.SendEvent(this, new MarginCallEventArgs(account, accountLevel));
            }
        }

        public OrderFulfillmentPlan MatchOnExistingPositions(Order order)
        {
            if (order.ForceOpen)
                return OrderFulfillmentPlan.Force(order, true);

            if (order.PositionsToBeClosed.Any())
                return OrderFulfillmentPlan.Force(order, false);

            var oppositeDirectionPositions = _ordersCache
                .Positions
                .GetPositionsByInstrumentAndAccount(order.AssetPairId, order.AccountId)
                .Where(p => p.Status == PositionStatus.Active && p.Direction == order.Direction.GetClosePositionDirection())
                .ToList();

            return OrderFulfillmentPlan.Create(order, oppositeDirectionPositions);
        }

        private void RejectOrder(Order order, OrderRejectReason reason, string message, string comment = null)
        {
            if (order.OrderType == OrderType.Market
                || reason != OrderRejectReason.NoLiquidity
                || order.PendingOrderRetriesCount >= _marginTradingSettings.PendingOrderRetriesThreshold)
            {
                order.Reject(reason, message, comment, _dateService.Now());

                _log.WriteWarning(
                    nameof(TradingEngine),
                    nameof(RejectOrder),
                    new
                    {
                        order,
                        reason,
                        message,
                        comment
                    }.ToJson());

                _orderRejectedEventChannel.SendEvent(this, new OrderRejectedEventArgs(order));
            }
            //TODO: think how to avoid infinite loop
            else if (!_ordersCache.TryGetOrderById(order.Id, out _)) // all pending orders should be returned to active state if there is no liquidity
            {
                order.CancelExecution(_dateService.Now());

                _ordersCache.Active.Add(order);

                var initialAdditionalInfo = order.AdditionalInfo;
                //to evade additional OnBehalf fee for this event
                order.AdditionalInfo = initialAdditionalInfo.MakeNonOnBehalf();

                _orderChangedEventChannel.SendEvent(this,
                    new OrderChangedEventArgs(order,
                        new OrderChangedMetadata { UpdatedProperty = OrderChangedProperty.None }));

                order.AdditionalInfo = initialAdditionalInfo;
            }
        }

        #region Orders waiting for execution

        private void ProcessOrdersWaitingForExecution(InstrumentBidAskPair quote)
        {
            //TODO: MTC-155
            //ProcessPendingOrdersMarginRecalc(instrument);

            var orders = GetPendingOrdersToBeExecuted(quote).GetSortedForExecution();

            if (!orders.Any())
                return;

            var correlationContext = _correlationContextAccessor.CorrelationContext;

            foreach (var order in orders)
            {
                _threadSwitcher.SwitchThread(async () =>
                {
                    _correlationContextAccessor.CorrelationContext = correlationContext;
                    await ExecutePendingOrder(order);
                });
            }
        }

        private IEnumerable<Order> GetPendingOrdersToBeExecuted(InstrumentBidAskPair quote)
        {
            var pendingOrders = _ordersCache.Active.GetOrdersByInstrument(quote.Instrument);

            foreach (var order in pendingOrders)
            {
                var price = quote.GetPriceForOrderDirection(order.Direction);

                var orderFulfillmentPlan = MatchOnExistingPositions(order);

                if (order.IsSuitablePriceForPendingOrder(price) &&
                    _orderValidator.CheckIfPendingOrderExecutionPossible(order.AssetPairId, order.OrderType, orderFulfillmentPlan.RequiresPositionOpening))
                {
                    if (quote.GetVolumeForOrderDirection(order.Direction) >= Math.Abs(orderFulfillmentPlan.UnfulfilledVolume))
                    {
                        _ordersCache.Active.Remove(order);
                        yield return order;
                    }
                    else //let's validate one more time, considering orderbook depth
                    {
                        var me = _meRouter.GetMatchingEngineForExecution(order);
                        var executionPriceInfo = me.GetBestPriceForOpen(order.AssetPairId, orderFulfillmentPlan.UnfulfilledVolume);

                        if (executionPriceInfo.price.HasValue && order.IsSuitablePriceForPendingOrder(executionPriceInfo.price.Value))
                        {
                            _ordersCache.Active.Remove(order);
                            yield return order;
                        }
                    }
                }

            }
        }

        public void ProcessExpiredOrders(DateTime operationIntervalEnd)
        {
            _cqrsSender.PublishEvent(new ExpirationProcessStartedEvent()
            {
                OperationIntervalEnd = operationIntervalEnd,
            });

            var pendingOrders = _ordersCache.Active.GetAllOrders();
            var now = _dateService.Now();

            foreach (var order in pendingOrders)
            {
                if (order.Validity.HasValue && operationIntervalEnd.Date > order.Validity.Value.Date)
                {
                    _ordersCache.Active.Remove(order);
                    order.Expire(now);
                    _orderCancelledEventChannel.SendEvent(
                        this,
                        new OrderCancelledEventArgs(
                            order,
                            new OrderCancelledMetadata { Reason = OrderCancellationReasonContract.Expired }));
                }
            }
        }

        //        private void ProcessPendingOrdersMarginRecalc(string instrument)
        //        {
        //            var pendingOrders = _ordersCache.GetPendingForMarginRecalc(instrument);
        //
        //            foreach (var pendingOrder in pendingOrders)
        //            {
        //                pendingOrder.UpdatePendingOrderMargin();
        //            }
        //        }

        #endregion


        #region Positions

        private void UpdatePositionsFxRates(InstrumentBidAskPair quote)
        {
            foreach (var position in _ordersCache.GetPositionsByFxAssetPairId(quote.Instrument))
            {
                var fxPrice = _cfdCalculatorService.GetPrice(quote.Bid, quote.Ask, position.FxToAssetPairDirection,
                    position.Volume * (position.ClosePrice - position.OpenPrice) > 0);

                position.UpdateCloseFxPrice(fxPrice);
            }
        }

        private async Task ProcessPositions(InstrumentBidAskPair quote, bool allowCommitStopOut)
        {
            var stopoutAccounts = UpdateClosePriceAndDetectStopout(quote);

            if (allowCommitStopOut)
            {
                foreach (var account in stopoutAccounts)
                    await CommitStopOut(account, quote);
            }
        }

        private List<MarginTradingAccount> UpdateClosePriceAndDetectStopout(InstrumentBidAskPair quote)
        {
            var positionsByAccounts = _ordersCache.Positions.GetPositionsByInstrument(quote.Instrument)
                .GroupBy(x => x.AccountId).ToDictionary(x => x.Key, x => x.ToArray());

            var accountsWithStopout = new List<MarginTradingAccount>();

            foreach (var accountPositions in positionsByAccounts)
            {
                var account = _accountsCacheService.Get(accountPositions.Key);
                var oldAccountLevel = account.GetAccountLevel();

                foreach (var position in accountPositions.Value)
                {
                    var closeOrderDirection = position.Volume.GetClosePositionOrderDirection();
                    var closePrice = quote.GetPriceForOrderDirection(closeOrderDirection);

                    if (quote.GetVolumeForOrderDirection(closeOrderDirection) < Math.Abs(position.Volume))
                    {
                        var defaultMatchingEngine = _meRouter.GetMatchingEngineForClose(position.OpenMatchingEngineId);

                        var orderbookPrice = defaultMatchingEngine.GetPriceForClose(position.AssetPairId, position.Volume,
                            position.ExternalProviderId);

                        if (orderbookPrice.HasValue)
                            closePrice = orderbookPrice.Value;
                    }

                    if (closePrice != 0)
                    {
                        position.UpdateClosePriceWithoutAccountUpdate(closePrice);

                        UpdateTrailingStops(position);
                    }
                }

                account.CacheNeedsToBeUpdated();

                var newAccountLevel = account.GetAccountLevel();

                if (newAccountLevel == AccountLevel.StopOut)
                    accountsWithStopout.Add(account);

                if (oldAccountLevel != newAccountLevel)
                {
                    _marginCallEventChannel.SendEvent(this, new MarginCallEventArgs(account, newAccountLevel));
                }
            }

            return accountsWithStopout;
        }

        private void UpdateTrailingStops(Position position)
        {
            foreach (var trailingOrderId in position.GetTrailingStopOrderIds())
            {
                if (_ordersCache.TryGetOrderById(trailingOrderId, out var trailingOrder))
                {
                    var oldPrice = trailingOrder.Price;

                    trailingOrder.UpdateTrailingStopWithClosePrice(position.ClosePrice, () => _dateService.Now());

                    if (oldPrice != trailingOrder.Price)
                    {
                        _log.WriteInfo(nameof(TradingEngine), nameof(UpdateTrailingStops),
                            $"Price for trailing stop order {trailingOrder.Id} changed. " +
                            $"Old price: {oldPrice}. " +
                            $"New price: {trailingOrder.Price}");
                    }
                }
            }
        }

        private async Task CommitStopOut(MarginTradingAccount account, InstrumentBidAskPair quote)
        {
            if (await _accountsCacheService.IsInLiquidation(account.Id))
                return;

            var liquidationType = account.GetUsedMargin() == account.GetCurrentlyUsedMargin()
                ? LiquidationType.Normal
                : LiquidationType.Mco;

            _cqrsSender.SendCommandToSelf(new StartLiquidationInternalCommand
            {
                OperationId = _identityGenerator.GenerateGuid(),
                AccountId = account.Id,
                CreationTime = _dateService.Now(),
                QuoteInfo = quote?.ToJson(),
                LiquidationType = liquidationType,
                OriginatorType = OriginatorType.System,
            });

            _stopOutEventChannel.SendEvent(this, new StopOutEventArgs(account));
        }

        public async Task<(PositionCloseResult, Order)> ClosePositionsAsync(PositionsCloseData closeData, bool specialLiquidationEnabled)
        {
            var me = closeData.MatchingEngine ??
                     _meRouter.GetMatchingEngineForClose(closeData.OpenMatchingEngineId);

            var initialParameters = await _orderValidator.GetOrderInitialParameters(closeData.AssetPairId,
                closeData.AccountId);

            var account = _accountsCacheService.Get(closeData.AccountId);

            var positionIds = new List<string>();
            var now = _dateService.Now();
            var volume = 0M;

            var positions = closeData.Positions;

            if (closeData.Modality != OrderModality.Liquidation_MarginCall && closeData.Modality != OrderModality.Liquidation_CorporateAction)
            {
                positions = positions
                    .Where(p => p.Value.Status == PositionStatus.Active)
                    .ToSortedList(x => x.Key, x => x.Value);
            }

            foreach (var position in positions)
            {
                var (closingStarted, reasonIfNot) = position.Value.TryStartClosing(now,
                    PositionCloseReason.Close,
                    closeData.Originator,
                    string.Empty);

                closingStarted = closingStarted || position.Value.Status == PositionStatus.Closing;

                if (!closingStarted)
                {
                    _log.WriteWarning(nameof(ClosePositionsAsync), position.ToJson(),
                        $"Couldn't start position closing due to: {reasonIfNot}");
                }

                if (closingStarted
                    ||
                    closeData.Modality == OrderModality.Liquidation_MarginCall
                    ||
                    closeData.Modality == OrderModality.Liquidation_CorporateAction)
                {
                    positionIds.Add(position.Value.Id);
                    volume += position.Value.Volume;
                }
            }

            if (!positionIds.Any())
            {
                if (closeData.Positions.Any(p => p.Value.Status == PositionStatus.Closing))
                {
                    return (PositionCloseResult.ClosingIsInProgress, null);
                }

                throw new Exception("No active positions to close");
            }

            var order = new Order(initialParameters.Id,
                initialParameters.Code,
                closeData.AssetPairId,
                -volume,
                initialParameters.Now,
                initialParameters.Now,
                null,
                account.Id,
                account.TradingConditionId,
                account.BaseAssetId,
                null,
                closeData.EquivalentAsset,
                OrderFillType.FillOrKill,
                $"Close positions: {string.Join(",", positionIds)}. {closeData.Comment}",
                account.LegalEntity,
                false,
                OrderType.Market,
                null,
                null,
                closeData.Originator,
                initialParameters.EquivalentPrice,
                initialParameters.FxPrice,
                initialParameters.FxAssetPairId,
                initialParameters.FxToAssetPairDirection,
                OrderStatus.Placed,
                closeData.AdditionalInfo,
                positionIds,
                closeData.ExternalProviderId);

            _orderPlacedEventChannel.SendEvent(this, new OrderPlacedEventArgs(order));

            order = await ExecuteOrderByMatchingEngineAsync(order, me, true, closeData.Modality);

            if (order.IsExecutionNotStarted)
            {
                if (specialLiquidationEnabled && order.RejectReason == OrderRejectReason.NoLiquidity)
                {
                    var command = new StartSpecialLiquidationInternalCommand
                    {
                        OperationId = Guid.NewGuid().ToString(),
                        CreationTime = _dateService.Now(),
                        AccountId = order.AccountId,
                        PositionIds = order.PositionsToBeClosed.ToArray(),
                        AdditionalInfo = order.AdditionalInfo,
                        OriginatorType = order.Originator
                    };

                    _cqrsSender.SendCommandToSelf(command);

                    return (PositionCloseResult.ClosingStarted, null);
                }

                foreach (var position in closeData.Positions)
                {
                    if (position.Value.Status == PositionStatus.Closing)
                        position.Value.CancelClosing(_dateService.Now());
                }

                _log.WriteWarning(nameof(ClosePositionsAsync), order,
                    $"Order {order.Id} was not executed. Closing of positions canceled");

                throw new Exception($"Positions were not closed. Reason: {order.RejectReasonText}");
            }

            return (PositionCloseResult.Closed, order);
        }

        [ItemNotNull]
        public async Task<Dictionary<string, (PositionCloseResult, Order)>> ClosePositionsGroupAsync(
            IList<Position> positions,
            [NotNull] string operationId,
            OriginatorType originator,
            PositionDirection? direction = null,
            string additionalInfo = null)
        {
            #region Validations

            if (positions == null || !positions.Any())
            {
                return new Dictionary<string, (PositionCloseResult, Order)>();
            }

            var accountId = positions.First().AccountId;
            if (positions.Any(p => p.AccountId != accountId))
            {
                throw new PositionGroupValidationException(
                    "Positions list contains elements of multiple accounts",
                    PositionGroupValidationError.MultipleAccounts);
            }

            // if direction was not passed in we have to ensure all the positions in the list are of single direction
            if (!direction.HasValue)
            {
                direction = positions.First().Direction;
                if (positions.Any(p => p.Direction != direction))
                {
                    throw new PositionGroupValidationException(
                        "Direction was not explicitly specified and positions list contains elements of both directions",
                        PositionGroupValidationError.MultipleDirections);
                }
            }

            var assetPairId = positions.First().AssetPairId;
            if (positions.Any(p => p.AssetPairId != assetPairId))
            {
                throw new PositionGroupValidationException(
                    "Positions list contains elements of multiple instruments",
                    PositionGroupValidationError.MultipleInstruments);
            }

            #endregion

            var positionGroups = positions
                .Where(p => p.Direction == direction)
                // grouping is required to optimize price requests but in fact we'll always have a SINGLE group here
                .GroupBy(p => (p.AssetPairId, p.AccountId, p.Direction, p.OpenMatchingEngineId, p.ExternalProviderId, p.EquivalentAsset))
                .Select(gr => new PositionsCloseData(
                    gr.LargestPnlFirst().FreezeOrder(),
                    gr.Key.AccountId,
                    gr.Key.AssetPairId,
                    gr.Key.OpenMatchingEngineId,
                    gr.Key.ExternalProviderId,
                    originator,
                    additionalInfo,
                    gr.Key.EquivalentAsset,
                    string.Empty));

            var result = new Dictionary<string, (PositionCloseResult, Order)>();

            foreach (var positionGroup in positionGroups)
            {
                try
                {
                    ValidationHelper.ValidateAccountId(positionGroup, accountId);

                    var closeResult = await ClosePositionsAsync(positionGroup, true);

                    foreach (var position in positionGroup.Positions)
                    {
                        result.Add(position.Value.Id, closeResult);
                    }
                }
                catch (Exception ex)
                {
                    await _log.WriteWarningAsync(nameof(ClosePositionsAsync),
                        positionGroup.ToJson(),
                        $"Failed to close positions {string.Join(",", positionGroup.Positions.Select(p => p.Value.Id))}",
                        ex);

                    foreach (var position in positionGroup.Positions)
                    {
                        result.Add(position.Value.Id, (PositionCloseResult.FailedToClose, null));
                    }
                }
            }

            return result;
        }

        [ItemNotNull]
        public async Task<Dictionary<string, (PositionCloseResult, Order)>> ClosePositionsGroupAsync(string accountId,
            string assetPairId,
            PositionDirection? direction,
            OriginatorType originator,
            string additionalInfo)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new AccountValidationException(AccountValidationError.AccountEmpty);
            }

            bool closeAll = string.IsNullOrEmpty(assetPairId);

            var operationId = _identityGenerator.GenerateGuid();
            if (closeAll)
            {
                return _liquidationHelper.StartLiquidation(accountId, originator, additionalInfo, operationId);
            }

            // Closing group of positions (asset and direction are always defined)
            // let's ensure direction is always passed in
            if (!direction.HasValue)
            {
                throw new PositionGroupValidationException(
                    "When closing group of positions direction is mandatory",
                    PositionGroupValidationError.DirectionEmpty);
            }

            var mandatoryDirection = direction.Value;

            var positions = _ordersCache
                .Positions
                .GetPositionsByInstrumentAndAccount(assetPairId, accountId)
                .ToList();

            return await ClosePositionsGroupAsync(positions, operationId, originator, mandatoryDirection, additionalInfo);
        }

        public async Task<(PositionCloseResult, Order)[]> LiquidatePositionsUsingSpecialWorkflowAsync(
            IMatchingEngineBase me, string[] positionIds, string additionalInfo,
            OriginatorType originator, OrderModality modality)
        {
            var positionsToClose = _ordersCache.Positions.GetAllPositions()
                .Where(x => positionIds.Contains(x.Id)).ToList();

            var positionGroups = positionsToClose
                .GroupBy(p => (p.AssetPairId, p.AccountId, p.Direction, p.OpenMatchingEngineId, p.ExternalProviderId, p.EquivalentAsset))
                .Select(gr => new PositionsCloseData(
                    gr.FreezeOrder(),
                    gr.Key.AccountId,
                    gr.Key.AssetPairId,
                    gr.Key.OpenMatchingEngineId,
                    gr.Key.ExternalProviderId,
                    originator,
                    additionalInfo,
                    gr.Key.EquivalentAsset,
                    "Special Liquidation",
                    me,
                    modality));

            var failedPositionIds = new List<string>();

            var closeOrderList = (await Task.WhenAll(positionGroups
                .Select(async group =>
                {
                    try
                    {
                        return await ClosePositionsAsync(group, false);
                    }
                    catch (Exception)
                    {
                        failedPositionIds.AddRange(group.Positions.Select(p => p.Value.Id));
                        return default;
                    }
                }))).Where(x => x != default).ToArray();

            if (failedPositionIds.Any())
            {
                throw new Exception($"Special liquidation failed to close these positions: {string.Join(", ", failedPositionIds)}");
            }

            return closeOrderList;
        }

        public Order CancelPendingOrder(string orderId, string additionalInfo,
            string comment = null, OrderCancellationReason reason = OrderCancellationReason.None)
        {
            var order = _ordersCache.GetOrderById(orderId);

            if (order.Status == OrderStatus.Inactive)
            {
                _ordersCache.Inactive.Remove(order);
            }
            else if (order.Status == OrderStatus.Active)
            {
                _ordersCache.Active.Remove(order);
            }
            else
            {
                throw new OrderValidationException($"Order in state {order.Status} can not be cancelled",
                    OrderValidationError.IncorrectStatusWhenCancel);
            }

            order.Cancel(_dateService.Now(), additionalInfo);

            var metadata = new OrderCancelledMetadata { Reason = reason.ToType<OrderCancellationReasonContract>() };
            _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(order, metadata));

            return order;
        }

        #endregion


        public async Task ChangeOrderAsync(string orderId, decimal price, OriginatorType originator,
            string additionalInfo, bool? forceOpen = null)
        {
            var order = _ordersCache.GetOrderById(orderId);

            var assetPair = _orderValidator.GetAssetPairIfAvailableForTrading(order.AssetPairId, order.OrderType,
                order.ForceOpen, false, true);
            price = Math.Round(price, assetPair.Accuracy);
            await _log.WriteInfoAsync(
                nameof(TradingEngine),
                nameof(ChangeOrderAsync),
                new { Order = order, Price = price, ForceOpen = forceOpen }.ToJson(),
                "BUGS-2954: Changing order price");

            _orderValidator.ValidateOrderPriceChange(order, price);
            _orderValidator.ValidateForceOpenChange(order, forceOpen);

            await _log.WriteInfoAsync(
                nameof(TradingEngine),
                nameof(ChangeOrderAsync),
                new { Order = order, Price = price, Forceopen = forceOpen }.ToJson(),
                "BUGS-2954: Order price change accepted");

            if (order.Price != price)
            {
                var oldPrice = order.Price;

                order.ChangePrice(price, _dateService.Now(), originator, additionalInfo, true);

                var metadata = new OrderChangedMetadata
                {
                    UpdatedProperty = OrderChangedProperty.Price,
                    OldValue = oldPrice.HasValue ? oldPrice.Value.ToString("F5") : string.Empty
                };

                _orderChangedEventChannel.SendEvent(this, new OrderChangedEventArgs(order, metadata));
            }

            if (forceOpen.HasValue && forceOpen.Value != order.ForceOpen)
            {
                var oldForceOpen = order.ForceOpen;

                order.ChangeForceOpen(forceOpen.Value, _dateService.Now(), originator, additionalInfo);

                var metadata = new OrderChangedMetadata
                {
                    UpdatedProperty = OrderChangedProperty.ForceOpen,
                    OldValue = oldForceOpen.ToString(),
                };

                _orderChangedEventChannel.SendEvent(this, new OrderChangedEventArgs(order, metadata));
            }

            await ExecutePendingOrderIfNeededAsync(order);
        }

        public async Task ChangeOrderValidityAsync(string orderId, DateTime validity, OriginatorType originator,
            string additionalInfo)
        {
            var order = _ordersCache.GetOrderById(orderId);

            _orderValidator.ValidateValidity(validity, order.OrderType);

            if (order.Validity != validity)
            {
                var oldValidity = order.Validity;

                order.ChangeValidity(validity, _dateService.Now(), originator, additionalInfo);

                var metadata = new OrderChangedMetadata
                {
                    UpdatedProperty = OrderChangedProperty.Validity,
                    OldValue = oldValidity.HasValue ? oldValidity.Value.ToString("g") : "GTC"
                };

                _orderChangedEventChannel.SendEvent(this, new OrderChangedEventArgs(order, metadata));
            }

            await ExecutePendingOrderIfNeededAsync(order);
        }

        public async Task RemoveOrderValidityAsync(string orderId, OriginatorType originator, string additionalInfo)
        {
            var order = _ordersCache.GetOrderById(orderId);

            if (order.Validity != null)
            {
                var oldValidity = order.Validity;

                order.ChangeValidity(null, _dateService.Now(), originator, additionalInfo);

                var metadata = new OrderChangedMetadata
                {
                    UpdatedProperty = OrderChangedProperty.Validity,
                    OldValue = oldValidity.Value.ToString("g")
                };

                _orderChangedEventChannel.SendEvent(this, new OrderChangedEventArgs(order, metadata));
            }

            await ExecutePendingOrderIfNeededAsync(order);
        }

        private async Task ExecutePendingOrderIfNeededAsync(Order order)
        {
            if (order.Status == OrderStatus.Active &&
                   _quoteCacheService.TryGetQuoteById(order.AssetPairId, out var pair))
            {
                var price = pair.GetPriceForOrderDirection(order.Direction);

                if (!_assetPairDayOffService.IsAssetTradingDisabled(order.AssetPairId) //!_assetPairDayOffService.ArePendingOrdersDisabled(order.AssetPairId))
                    && order.IsSuitablePriceForPendingOrder(price))
                {
                    _ordersCache.Active.Remove(order);
                    await ExecutePendingOrder(order);
                }
            }
        }

        int IEventConsumer.ConsumerRank => 101;

        void IEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            ProcessPositions(ea.BidAskPair, !ea.IsEod).GetAwaiter().GetResult();
            ProcessOrdersWaitingForExecution(ea.BidAskPair);
        }

        void IEventConsumer<FxBestPriceChangeEventArgs>.ConsumeEvent(object sender, FxBestPriceChangeEventArgs ea)
        {
            UpdatePositionsFxRates(ea.BidAskPair);
        }
    }
}
