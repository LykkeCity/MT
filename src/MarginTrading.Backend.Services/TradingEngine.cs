using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    public sealed class TradingEngine : ITradingEngine, IAsyncEventConsumer<BestPriceChangeEventArgs>
    {
        private readonly IEventChannel<MarginCallEventArgs> _marginCallEventChannel;
        private readonly IEventChannel<StopOutEventArgs> _stopoutEventChannel;
        private readonly IEventChannel<OrderPlacedEventArgs> _orderPlacedEventChannel;
        private readonly IEventChannel<OrderClosedEventArgs> _orderClosedEventChannel;
        private readonly IEventChannel<OrderCancelledEventArgs> _orderCancelledEventChannel;
        private readonly IEventChannel<OrderLimitsChangedEventArgs> _orderLimitsChangesEventChannel;
        private readonly IEventChannel<OrderClosingEventArgs> _orderClosingEventChannel;
        private readonly IEventChannel<OrderActivatedEventArgs> _orderActivatedEventChannel;
        private readonly IEventChannel<OrderRejectedEventArgs> _orderRejectedEventChannel;

        private readonly IQuoteCacheService _quoteCashService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly ICommissionService _swapCommissionService;
        private readonly IValidateOrderService _validateOrderService;
        private readonly IEquivalentPricesService _equivalentPricesService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly IMatchingEngineRouter _meRouter;
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IContextFactory _contextFactory;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly ILog _log;

        public TradingEngine(
            IEventChannel<MarginCallEventArgs> marginCallEventChannel,
            IEventChannel<StopOutEventArgs> stopoutEventChannel,
            IEventChannel<OrderPlacedEventArgs> orderPlacedEventChannel,
            IEventChannel<OrderClosedEventArgs> orderClosedEventChannel,
            IEventChannel<OrderCancelledEventArgs> orderCancelledEventChannel, 
            IEventChannel<OrderLimitsChangedEventArgs> orderLimitsChangesEventChannel,
            IEventChannel<OrderClosingEventArgs> orderClosingEventChannel,
            IEventChannel<OrderActivatedEventArgs> orderActivatedEventChannel, 
            IEventChannel<OrderRejectedEventArgs> orderRejectedEventChannel,
            
            IValidateOrderService validateOrderService,
            IQuoteCacheService quoteCashService,
            IAccountUpdateService accountUpdateService,
            ICommissionService swapCommissionService,
            IEquivalentPricesService equivalentPricesService,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IAccountAssetsCacheService accountAssetsCacheService,
            IMatchingEngineRouter meRouter,
            IThreadSwitcher threadSwitcher,
            IContextFactory contextFactory,
            IAssetPairDayOffService assetPairDayOffService,
            ILog log)
        {
            _marginCallEventChannel = marginCallEventChannel;
            _stopoutEventChannel = stopoutEventChannel;
            _orderPlacedEventChannel = orderPlacedEventChannel;
            _orderClosedEventChannel = orderClosedEventChannel;
            _orderCancelledEventChannel = orderCancelledEventChannel;
            _orderActivatedEventChannel = orderActivatedEventChannel;
            _orderClosingEventChannel = orderClosingEventChannel;
            _orderLimitsChangesEventChannel = orderLimitsChangesEventChannel;
            _orderRejectedEventChannel = orderRejectedEventChannel;

            _quoteCashService = quoteCashService;
            _accountUpdateService = accountUpdateService;
            _swapCommissionService = swapCommissionService;
            _validateOrderService = validateOrderService;
            _equivalentPricesService = equivalentPricesService;
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _accountAssetsCacheService = accountAssetsCacheService;
            _meRouter = meRouter;
            _threadSwitcher = threadSwitcher;
            _contextFactory = contextFactory;
            _assetPairDayOffService = assetPairDayOffService;
            _log = log;
        }

        public async Task<Order> PlaceOrderAsync(Order order)
        {
            try
            {
                _validateOrderService.Validate(order);

                if (order.ExpectedOpenPrice.HasValue)
                {
                    PlacePendingOrder(order);
                    return order;
                }

                return await PlaceOrderByMarketPrice(order);
            }
            catch (ValidateOrderException ex)
            {
                RejectOrder(order, ex.RejectReason, ex.Message, ex.Comment);
                return order;
            }
        }

        private Task<Order> PlaceMarketOrderByMatchingEngineAsync(Order order, IMatchingEngineBase matchingEngine)
        {
            order.OpenOrderbookId = matchingEngine.Id;
            order.MatchingEngineMode = matchingEngine.Mode;
            
            matchingEngine.MatchMarketOrderForOpen(order, async matchedOrders =>
            {
                if (!matchedOrders.Any())
                {
                    order.CloseDate = DateTime.UtcNow;
                    order.Status = OrderStatus.Rejected;
                    order.RejectReason = OrderRejectReason.NoLiquidity;
                    order.RejectReasonText = "No orders to match";
                    return false;
                }

                if (matchedOrders.SummaryVolume < Math.Abs(order.Volume) && order.FillType == OrderFillType.FillOrKill)
                {
                    order.CloseDate = DateTime.UtcNow;
                    order.Status = OrderStatus.Rejected;
                    order.RejectReason = OrderRejectReason.NoLiquidity;
                    order.RejectReasonText = "Not fully matched";
                    return false;
                }

                try
                {
                    await CheckIfWeCanOpenPosition(order, matchedOrders);
                }
                catch (ValidateOrderException e)
                {
                    order.CloseDate = DateTime.UtcNow;
                    order.Status = OrderStatus.Rejected;
                    order.RejectReason = e.RejectReason;
                    order.RejectReasonText = e.Message;
                    return false;
                }

                _equivalentPricesService.EnrichOpeningOrder(order);
                
                MakeOrderActive(order);

                return true;
            });

            if (order.Status == OrderStatus.Rejected)
            {
                _orderRejectedEventChannel.SendEvent(this, new OrderRejectedEventArgs(order));
            }

            return Task.FromResult(order);
        }

        private void RejectOrder(Order order, OrderRejectReason reason, string message, string comment = null)
        {
            order.CloseDate = DateTime.UtcNow;
            order.Status = OrderStatus.Rejected;
            order.RejectReason = reason;
            order.RejectReasonText = message;
            order.Comment = comment;
            _orderRejectedEventChannel.SendEvent(this, new OrderRejectedEventArgs(order));
        }

        private Task<Order> PlaceOrderByMarketPrice(Order order)
        {
            try
            {
                var me = _meRouter.GetMatchingEngineForOpen(order);

                return PlaceMarketOrderByMatchingEngineAsync(order, me);
            }
            catch (QuoteNotFoundException ex)
            {
                RejectOrder(order, OrderRejectReason.NoLiquidity, ex.Message);
                return Task.FromResult(order);
            }
            catch (Exception ex)
            {
                RejectOrder(order, OrderRejectReason.TechnicalError, ex.Message);
                _log.WriteError(nameof(TradingEngine), nameof(PlaceOrderByMarketPrice), ex);
                return Task.FromResult(order);
            }
        }

        private void MakeOrderActive(Order order)
        {
            order.OpenDate = DateTime.UtcNow;
            order.Status = OrderStatus.Active;

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            _swapCommissionService.SetCommissionRates(account.TradingConditionId, account.BaseAssetId, order);
            _ordersCache.ActiveOrders.AddAsync(order);
            _orderActivatedEventChannel.SendEvent(this, new OrderActivatedEventArgs(order));
        }

        private async Task CheckIfWeCanOpenPosition(Order order, MatchedOrderCollection matchedOrders)
        {
            var accountAsset = _accountAssetsCacheService.GetAccountAsset(order.TradingConditionId, order.AccountAssetId, order.Instrument);
            await _validateOrderService.ValidateInstrumentPositionVolume(accountAsset, order);

            order.MatchedOrders.AddRange(matchedOrders);
            order.OpenPrice = Math.Round(order.MatchedOrders.WeightedAveragePrice, order.AssetAccuracy);

            var defaultMatchingEngine = _meRouter.GetMatchingEngineForClose(order);

            var closePrice = defaultMatchingEngine.GetPriceForClose(order);

            if (!closePrice.HasValue)
            {
                throw new ValidateOrderException(OrderRejectReason.NoLiquidity, "No orders to match for close");
            }
            
            order.UpdateClosePrice(Math.Round(closePrice.Value, order.AssetAccuracy));

            //TODO: very strange check.. think about it one more time
            var guessAccount = await _accountUpdateService.GuessAccountWithNewActiveOrder(order);
            var guessAccountLevel = guessAccount.GetAccountLevel();

            if (guessAccountLevel != AccountLevel.None)
            {
                order.OpenPrice = 0;
                order.ClosePrice = 0;
                order.MatchedOrders = new MatchedOrderCollection();
            }

            if (guessAccountLevel == AccountLevel.MarginCall)
            {
                throw new ValidateOrderException(OrderRejectReason.AccountInvalidState,
                    "Opening the position will lead to account Margin Call level");
            }

            if (guessAccountLevel == AccountLevel.StopOUt)
            {
                throw new ValidateOrderException(OrderRejectReason.AccountInvalidState,
                    "Opening the position will lead to account Stop Out level");
            }
        }

        private void PlacePendingOrder(Order order)
        {
            var me = _meRouter.GetMatchingEngineForOpen(order);
            order.MatchingEngineMode = me.Mode;
            
            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(PlacePendingOrder)}"))
                _ordersCache.WaitingForExecutionOrders.AddAsync(order);

            _orderPlacedEventChannel.SendEvent(this, new OrderPlacedEventArgs(order));
        }

        #region Orders waiting for execution
        
        private async Task ProcessOrdersWaitingForExecution(string instrument)
        {
            await ProcessPendingOrdersMarginRecalc(instrument);
            
            var orders = (await GetPendingOrdersToBeExecuted(instrument)).ToArray();

            if (orders.Length == 0)
                return;

            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(ProcessOrdersWaitingForExecution)}"))
            {
                foreach (var order in orders)
                    await _ordersCache.WaitingForExecutionOrders.RemoveAsync(order);
            }

            //TODO: think how to make sure that we don't loose orders
            _threadSwitcher.SwitchThread(async () =>
            {
                foreach (var order in orders)
                    await PlaceOrderByMarketPrice(order);
            });

        }

        private async Task<IEnumerable<Order>> GetPendingOrdersToBeExecuted(string instrument)
        {
            var pendingOrders = (await _ordersCache.WaitingForExecutionOrders.GetOrdersByInstrument(instrument))
                .OrderBy(item => item.CreateDate);

            var result = new List<Order>();
            foreach (var order in pendingOrders)
            {
                if (!_quoteCashService.TryGetQuoteById(order.Instrument, out var pair)) 
                    continue;
                var price = pair.GetPriceForOrderType(order.GetCloseType());

                if (order.IsSuitablePriceForPendingOrder(price) &&
                    !_assetPairDayOffService.ArePendingOrdersDisabled(order.Instrument))
                    result.Add(order);
            }

            return result;
        }

        private async Task ProcessPendingOrdersMarginRecalc(string instrument)
        {
            var pendingOrders = await _ordersCache.GetPendingForMarginRecalc(instrument);

            foreach (var pendingOrder in pendingOrders)
            {
                pendingOrder.UpdatePendingOrderMargin();
            }
        }

        #endregion

        #region Active orders

        private async Task ProcessOrdersActive(string instrument)
        {
            var stopoutAccounts = (await UpdateClosePriceAndDetectStopout(instrument)).ToArray();
            foreach (var account in stopoutAccounts)
                await CommitStopout(account);

            foreach (var order in await _ordersCache.ActiveOrders.GetOrdersByInstrument(instrument))
            {
                if (order.IsStopLoss())
                    SetOrderToClosingState(order, OrderCloseReason.StopLoss);
                else if (order.IsTakeProfit())
                    SetOrderToClosingState(order, OrderCloseReason.TakeProfit);
            }

            await ProcessOrdersClosing(instrument);
        }

        private async Task<IEnumerable<MarginTradingAccount>> UpdateClosePriceAndDetectStopout(string instrument)
        {
            var openOrders = (await _ordersCache.ActiveOrders.GetOrdersByInstrument(instrument))
                .GroupBy(x => x.AccountId).ToDictionary(x => x.Key, x => x.ToArray());

            var accounts = new List<MarginTradingAccount>();
            foreach (var accountOrders in openOrders)
            {
                var anyOrder = accountOrders.Value.FirstOrDefault();
                if (null == anyOrder)
                    continue;

                var account = _accountsCacheService.Get(anyOrder.ClientId, anyOrder.AccountId);
                var oldAccountLevel = account.GetAccountLevel();

                foreach (var order in accountOrders.Value)
                {
                    var defaultMatchingEngine = _meRouter.GetMatchingEngineForClose(order);

                    var closePrice = defaultMatchingEngine.GetPriceForClose(order);

                    if (closePrice.HasValue)
                    {
                        order.UpdateClosePrice(Math.Round(closePrice.Value, order.AssetAccuracy));
                    }
                }

                var newAccountLevel = account.GetAccountLevel();

                if (oldAccountLevel != newAccountLevel)
                {
                    NotifyAccountLevelChanged(account, newAccountLevel);

                    if (newAccountLevel == AccountLevel.StopOUt)
                        accounts.Add(account);
                }
            }

            return accounts;
        }

        private async Task CommitStopout(MarginTradingAccount account)
        {
            var pendingOrders = await _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(account.Id);

            var cancelledPendingOrders = new List<Order>();
            
            foreach (var pendingOrder in pendingOrders)
            {
                cancelledPendingOrders.Add(pendingOrder);
                await CancelPendingOrder(pendingOrder.Id, OrderCloseReason.CanceledBySystem, "Stop out");
            }
            
            var activeOrders = await _ordersCache.ActiveOrders.GetOrdersByAccountIds(account.Id);
            
            var ordersToClose = new List<Order>();
            var newAccountUsedMargin = account.GetUsedMargin();

            foreach (var order in activeOrders.OrderBy(o => o.GetTotalFpl()))
            {
                if (newAccountUsedMargin <= 0 ||
                    account.GetTotalCapital() / newAccountUsedMargin > account.GetMarginCallLevel())
                    break;
                
                ordersToClose.Add(order);
                newAccountUsedMargin -= order.GetMarginMaintenance();
            }

            if (!ordersToClose.Any() && !cancelledPendingOrders.Any())
                return;

            _stopoutEventChannel.SendEvent(this,
                new StopOutEventArgs(account, ordersToClose.Concat(cancelledPendingOrders).ToArray()));

            foreach (var order in ordersToClose)
                SetOrderToClosingState(order, OrderCloseReason.StopOut);
        }

        private void SetOrderToClosingState(Order order, OrderCloseReason reason)
        {
            order.Status = OrderStatus.Closing;
            order.StartClosingDate = DateTime.UtcNow;
            order.CloseReason = reason;

            _ordersCache.ClosingOrders.AddAsync(order);
            _ordersCache.ActiveOrders.RemoveAsync(order);
        }

        private void NotifyAccountLevelChanged(MarginTradingAccount account, AccountLevel newAccountLevel)
        {
            switch (newAccountLevel)
            {
                case AccountLevel.MarginCall:
                    _marginCallEventChannel.SendEvent(this, new MarginCallEventArgs(account));
                    break;
            }
        }

        private Task<Order> CloseActiveOrderByMatchingEngineAsync(Order order, IMatchingEngineBase matchingEngine, OrderCloseReason reason, string comment)
        {
            order.CloseOrderbookId = matchingEngine.Id;
            order.StartClosingDate = DateTime.UtcNow;
            order.CloseReason = reason;
            order.Comment = comment;

            matchingEngine.MatchMarketOrderForClose(order, matchedOrders =>
            {
                if (!matchedOrders.Any())
                {
                    order.CloseRejectReasonText = "No orders to match";
                    return false;
                }
                
                order.MatchedCloseOrders.AddRange(matchedOrders);

                _equivalentPricesService.EnrichClosingOrder(order);

                if (!order.GetIsCloseFullfilled())
                {
                    order.Status = OrderStatus.Closing;
                    _ordersCache.ActiveOrders.RemoveAsync(order);
                    _ordersCache.ClosingOrders.AddAsync(order);
                    _orderClosingEventChannel.SendEvent(this, new OrderClosingEventArgs(order));
                }
                else
                {
                    order.Status = OrderStatus.Closed;
                    order.CloseDate = DateTime.UtcNow;
                    _ordersCache.ActiveOrders.RemoveAsync(order);
                    _orderClosedEventChannel.SendEvent(this, new OrderClosedEventArgs(order));
                }

                return true;
            });

            return Task.FromResult(order);
        }

        public async Task<Order> CloseActiveOrderAsync(string orderId, OrderCloseReason reason, string comment = null)
        {
            var order = await GetActiveOrderForClose(orderId);

            var me = _meRouter.GetMatchingEngineForClose(order);

            return await CloseActiveOrderByMatchingEngineAsync(order, me, reason, comment);
        }

        public async Task<Order> CancelPendingOrder(string orderId, OrderCloseReason reason, string comment = null)
        {
            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(CancelPendingOrder)}"))
            {
                var order = await _ordersCache.WaitingForExecutionOrders.GetOrderById(orderId);
                CancelWaitingForExecutionOrder(order, reason, comment);
                return order;
            }
        }
        #endregion


        private async Task<Order> GetActiveOrderForClose(string orderId)
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(TradingEngine)}.{nameof(GetActiveOrderForClose)}"))
            {
                return await _ordersCache.ActiveOrders.GetOrderById(orderId);
            }
        }

        private void CancelWaitingForExecutionOrder(Order order, OrderCloseReason reason, string comment)
        {
            order.Status = OrderStatus.Closed;
            order.CloseDate = DateTime.UtcNow;
            order.CloseReason = reason;
            order.Comment = comment;
            
            _ordersCache.WaitingForExecutionOrders.RemoveAsync(order);

            _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(order));
        }

        public async Task ChangeOrderLimits(string orderId, decimal? stopLoss, decimal? takeProfit, decimal? expectedOpenPrice)
        {
            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(ChangeOrderLimits)}"))
            {
                var order = await _ordersCache.GetOrderById(orderId);

                if (order.Status != OrderStatus.WaitingForExecution && expectedOpenPrice > 0)
                {
                    return;
                }

                var quote = _quoteCashService.GetQuote(order.Instrument);
                var tp = takeProfit == 0 ? null : takeProfit;
                var sl = stopLoss == 0 ? null : stopLoss;
                var expOpenPrice = expectedOpenPrice == 0 ? null : expectedOpenPrice;

                var accountAsset = _accountAssetsCacheService.GetAccountAsset(order.TradingConditionId,
                    order.AccountAssetId, order.Instrument);

                _validateOrderService.ValidateOrderStops(order.GetOrderType(), quote,
                    accountAsset.DeltaBid, accountAsset.DeltaAsk, tp, sl, expOpenPrice, order.AssetAccuracy);

                order.TakeProfit = tp.HasValue ? Math.Round(tp.Value, order.AssetAccuracy) : (decimal?)null;
                order.StopLoss = sl.HasValue ? Math.Round(sl.Value, order.AssetAccuracy) : (decimal?)null;
                order.ExpectedOpenPrice = expOpenPrice.HasValue ? Math.Round(expOpenPrice.Value, order.AssetAccuracy) : (decimal?)null;
                _orderLimitsChangesEventChannel.SendEvent(this, new OrderLimitsChangedEventArgs(order));
            }
        }

        public bool PingLock()
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(TradingEngine)}.{nameof(PingLock)}"))
            {
                return true;
            }
        }

        private async Task ProcessOrdersClosing(string instrument = null)
        {
            var closingOrders = string.IsNullOrEmpty(instrument)
                ? await _ordersCache.ClosingOrders.GetAllOrders()
                : await _ordersCache.ClosingOrders.GetOrdersByInstrument(instrument);

            if (closingOrders.Count == 0)
                return;

            foreach (var order in closingOrders)
            {
                var me = _meRouter.GetMatchingEngineForClose(order);

                ProcessOrdersClosingByMatchingEngine(order, me);
            }
        }

        private void ProcessOrdersClosingByMatchingEngine(Order order, IMatchingEngineBase matchingEngine)
        {
            order.CloseOrderbookId = matchingEngine.Id;
            
            matchingEngine.MatchMarketOrderForClose(order, matchedOrders =>
            {
                if (matchedOrders.Count == 0)
                {
                    //if order did not started to close yet and for any reason we did not close it now - make active
                    if (!order.MatchedCloseOrders.Any())
                    {
                        order.Status = OrderStatus.Active;
                        order.CloseRejectReasonText = "No orders to match";
                        
                        _ordersCache.ActiveOrders.AddAsync(order);
                        _ordersCache.ClosingOrders.RemoveAsync(order);
                    }

                    return false;
                }

                MakeOrderClosed(order, matchedOrders);

                return true;
            });
        }

        private void MakeOrderClosed(Order order, MatchedOrderCollection matchedOrders)
        {
            order.MatchedCloseOrders.AddRange(matchedOrders);

            if (order.GetIsCloseFullfilled())
            {
                order.Status = OrderStatus.Closed;
                order.CloseDate = DateTime.UtcNow;
                _ordersCache.ClosingOrders.RemoveAsync(order);
                _orderClosedEventChannel.SendEvent(this, new OrderClosedEventArgs(order));
            }
        }

        int IEventConsumer.ConsumerRank => 100;

        async Task IAsyncEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            await ProcessOrdersClosing(ea.BidAskPair.Instrument);
            await ProcessOrdersActive(ea.BidAskPair.Instrument);
            await ProcessOrdersWaitingForExecution(ea.BidAskPair.Instrument);
        }
    }
}
