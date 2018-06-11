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
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services
{
    public sealed class TradingEngine : ITradingEngine, IEventConsumer<BestPriceChangeEventArgs>
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
        private readonly ITradingInstrumentsCacheService _accountAssetsCacheService;
        private readonly IMatchingEngineRouter _meRouter;
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IContextFactory _contextFactory;
        private readonly IAssetPairDayOffService _assetPairDayOffService;
        private readonly ILog _log;
        private readonly IDateService _dateService;

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
            ITradingInstrumentsCacheService accountAssetsCacheService,
            IMatchingEngineRouter meRouter,
            IThreadSwitcher threadSwitcher,
            IContextFactory contextFactory,
            IAssetPairDayOffService assetPairDayOffService,
            ILog log,
            IDateService dateService)
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
            _dateService = dateService;
        }

        public async Task<Order> PlaceOrderAsync(Order order)
        {
            try
            {
                if (order.OrderType != OrderType.Market)
                {
                    PlacePendingOrder(order);
                    return order;
                }

                return await PlaceOrderByMarketPrice(order);
            }
            catch (ValidateOrderException ex)
            {
                //RejectOrder(order, ex.RejectReason, ex.Message, ex.Comment);
                return order;
            }
        }

        private async Task<Position> PlaceMarketOrderByMatchingEngineAsync(Position order, IMatchingEngineBase matchingEngine)
        {
            order.OpenOrderbookId = matchingEngine.Id;
            order.MatchingEngineMode = matchingEngine.Mode;
            
            await matchingEngine.MatchMarketOrderForOpenAsync(order, matchedOrders =>
            {
                if (!matchedOrders.Any())
                {
                    order.CloseDate = DateTime.UtcNow;
                    order.Status = PositionStatus.Rejected;
                    order.RejectReason = OrderRejectReason.NoLiquidity;
                    order.RejectReasonText = "No orders to match";
                    return false;
                }

                if (matchedOrders.SummaryVolume < Math.Abs(order.Volume) && order.FillType == OrderFillType.FillOrKill)
                {
                    order.CloseDate = DateTime.UtcNow;
                    order.Status = PositionStatus.Rejected;
                    order.RejectReason = OrderRejectReason.NoLiquidity;
                    order.RejectReasonText = "Not fully matched";
                    return false;
                }

                try
                {
                    CheckIfWeCanOpenPosition(order, matchedOrders);
                }
                catch (ValidateOrderException e)
                {
                    order.CloseDate = DateTime.UtcNow;
                    order.Status = PositionStatus.Rejected;
                    order.RejectReason = e.RejectReason;
                    order.RejectReasonText = e.Message;
                    return false;
                }

                _equivalentPricesService.EnrichOpeningOrder(order);
                
                MakeOrderActive(order);

                return true;
            });

            if (order.Status == PositionStatus.Rejected)
            {
                _orderRejectedEventChannel.SendEvent(this, new OrderRejectedEventArgs(order));
            }

            return order;
        }

        private void RejectOrder(Order order, OrderRejectReason reason, string message, string comment = null)
        {
            order.Reject(reason, message, comment, _dateService.Now());
            //_orderRejectedEventChannel.SendEvent(this, new OrderRejectedEventArgs(order));
        }

        private Task<Order> PlaceOrderByMarketPrice(Order order)
        {
            try
            {
                var me = _meRouter.GetMatchingEngineForOpen(order);

                //TODO: implement
                return null;
                //return PlaceMarketOrderByMatchingEngineAsync(order, me);
            }
            catch (QuoteNotFoundException ex)
            {
                //RejectOrder(order, OrderRejectReason.NoLiquidity, ex.Message);
                return Task.FromResult(order);
            }
            catch (Exception ex)
            {
                //RejectOrder(order, OrderRejectReason.TechnicalError, ex.Message);
                _log.WriteError(nameof(TradingEngine), nameof(PlaceOrderByMarketPrice), ex);
                return Task.FromResult(order);
            }
        }

        private void MakeOrderActive(Position order)
        {
            var now = DateTime.UtcNow;
            order.OpenDate = now;
            order.LastModified = now;
            order.Status = PositionStatus.Active;

            var account = _accountsCacheService.Get(order.AccountId);
            _swapCommissionService.SetCommissionRates(account.TradingConditionId, order);
            _ordersCache.ActiveOrders.Add(order);
            _orderActivatedEventChannel.SendEvent(this, new OrderActivatedEventArgs(order));
        }

        private void CheckIfWeCanOpenPosition(Position order, MatchedOrderCollection matchedOrders)
        {
            var accountAsset =
                _accountAssetsCacheService.GetTradingInstrument(order.TradingConditionId, order.Instrument);
            _validateOrderService.ValidateInstrumentPositionVolume(accountAsset, order);

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
            var guessAccount = _accountUpdateService.GuessAccountWithNewActiveOrder(order);
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
            order.SetMatchingEngine(me.Id);
            
            //TODO: implement
            //_ordersCache.WaitingForExecutionOrders.Add(order);

            //_orderPlacedEventChannel.SendEvent(this, new OrderPlacedEventArgs(order));
        }

        #region Orders waiting for execution
        
        private void ProcessOrdersWaitingForExecution(string instrument)
        {
            ProcessPendingOrdersMarginRecalc(instrument);
            
            var orders = GetPendingOrdersToBeExecuted(instrument).ToArray();

            if (orders.Length == 0)
                return;

            //using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(ProcessOrdersWaitingForExecution)}"))
            //{
                foreach (var order in orders)
                    _ordersCache.WaitingForExecutionOrders.Remove(order);
            //}

            //TODO: think how to make sure that we don't loose orders
            _threadSwitcher.SwitchThread(async () =>
            {
                //TODO: implement
                //foreach (var order in orders)
                //    await PlaceOrderByMarketPrice(order);
            });

        }

        private IEnumerable<Position> GetPendingOrdersToBeExecuted(string instrument)
        {
            var pendingOrders = _ordersCache.WaitingForExecutionOrders.GetOrdersByInstrument(instrument)
                .OrderBy(item => item.CreateDate);

            foreach (var order in pendingOrders)
            {
                InstrumentBidAskPair pair;

                if (_quoteCashService.TryGetQuoteById(order.Instrument, out pair))
                {
                    var price = pair.GetPriceForOrderType(order.GetCloseType());

                    if (order.IsSuitablePriceForPendingOrder(price) &&
                        !_assetPairDayOffService.ArePendingOrdersDisabled(order.Instrument))
                        yield return order;
                }
            }
        }

        private void ProcessPendingOrdersMarginRecalc(string instrument)
        {
            var pendingOrders = _ordersCache.GetPendingForMarginRecalc(instrument);

            foreach (var pendingOrder in pendingOrders)
            {
                pendingOrder.UpdatePendingOrderMargin();
            }
        }

        #endregion

        #region Active orders

        private void ProcessOrdersActive(string instrument)
        {
            var stopoutAccounts = UpdateClosePriceAndDetectStopout(instrument).ToArray();
            foreach (var account in stopoutAccounts)
                CommitStopout(account);

            foreach (var order in _ordersCache.ActiveOrders.GetOrdersByInstrument(instrument))
            {
                if (order.IsStopLoss())
                    SetOrderToClosingState(order, OrderCloseReason.StopLoss);
                else if (order.IsTakeProfit())
                    SetOrderToClosingState(order, OrderCloseReason.TakeProfit);
            }

            ProcessOrdersClosing(instrument);
        }

        private IEnumerable<MarginTradingAccount> UpdateClosePriceAndDetectStopout(string instrument)
        {
            var openOrders = _ordersCache.ActiveOrders.GetOrdersByInstrument(instrument)
                .GroupBy(x => x.AccountId).ToDictionary(x => x.Key, x => x.ToArray());

            foreach (var accountOrders in openOrders)
            {
                var anyOrder = accountOrders.Value.FirstOrDefault();
                if (null == anyOrder)
                    continue;

                var account = _accountsCacheService.Get(anyOrder.AccountId);
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
                        yield return account;
                }
            }
        }

        private void CommitStopout(MarginTradingAccount account)
        {
            var pendingOrders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(account.Id);

            var cancelledPendingOrders = new List<Position>();
            
            foreach (var pendingOrder in pendingOrders)
            {
                cancelledPendingOrders.Add(pendingOrder);
                CancelPendingOrder(pendingOrder.Id, OrderCloseReason.CanceledBySystem, "Stop out");
            }
            
            var activeOrders = _ordersCache.ActiveOrders.GetOrdersByAccountIds(account.Id);
            
            var ordersToClose = new List<Position>();
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

        private void SetOrderToClosingState(Position order, OrderCloseReason reason)
        {
            order.Status = PositionStatus.Closing;
            order.StartClosingDate = DateTime.UtcNow;
            order.CloseReason = reason;

            _ordersCache.ClosingOrders.Add(order);
            _ordersCache.ActiveOrders.Remove(order);
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

        private async Task<Position> CloseActiveOrderByMatchingEngineAsync(Position order, IMatchingEngineBase matchingEngine, OrderCloseReason reason, string comment)
        {
            order.CloseOrderbookId = matchingEngine.Id;
            order.StartClosingDate = DateTime.UtcNow;
            order.CloseReason = reason;
            order.Comment = comment;

            await matchingEngine.MatchMarketOrderForCloseAsync(order, matchedOrders =>
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
                    order.Status = PositionStatus.Closing;
                    _ordersCache.ActiveOrders.Remove(order);
                    _ordersCache.ClosingOrders.Add(order);
                    _orderClosingEventChannel.SendEvent(this, new OrderClosingEventArgs(order));
                }
                else
                {
                    order.Status = PositionStatus.Closed;
                    order.CloseDate = DateTime.UtcNow;
                    _ordersCache.ActiveOrders.Remove(order);
                    _orderClosedEventChannel.SendEvent(this, new OrderClosedEventArgs(order));
                }

                return true;
            });

            return order;
        }

        public Task<Position> CloseActiveOrderAsync(string orderId, OrderCloseReason reason, string comment = null)
        {
            var order = GetActiveOrderForClose(orderId);

            var me = _meRouter.GetMatchingEngineForClose(order);

            return CloseActiveOrderByMatchingEngineAsync(order, me, reason, comment);
        }

        public Position CancelPendingOrder(string orderId, OrderCloseReason reason, string comment = null)
        {
            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(CancelPendingOrder)}"))
            {
                var order = _ordersCache.WaitingForExecutionOrders.GetOrderById(orderId);
                CancelWaitingForExecutionOrder(order, reason, comment);
                return order;
            }
        }
        #endregion


        private Position GetActiveOrderForClose(string orderId)
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(TradingEngine)}.{nameof(GetActiveOrderForClose)}"))
                return _ordersCache.ActiveOrders.GetOrderById(orderId);
        }

        private void CancelWaitingForExecutionOrder(Position order, OrderCloseReason reason, string comment)
        {
            order.Status = PositionStatus.Closed;
            order.CloseDate = DateTime.UtcNow;
            order.CloseReason = reason;
            order.Comment = comment;
            
            _ordersCache.WaitingForExecutionOrders.Remove(order);

            _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(order));
        }

        public void ChangeOrderLimits(string orderId, decimal? stopLoss, decimal? takeProfit, decimal? expectedOpenPrice)
        {
            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(ChangeOrderLimits)}"))
            {
                var order = _ordersCache.GetOrderById(orderId);

                if (order.Status != PositionStatus.WaitingForExecution && expectedOpenPrice > 0)
                {
                    return;
                }

                var quote = _quoteCashService.GetQuote(order.Instrument);
                var tp = takeProfit == 0 ? null : takeProfit;
                var sl = stopLoss == 0 ? null : stopLoss;
                var expOpenPrice = expectedOpenPrice == 0 ? null : expectedOpenPrice;

                var accountAsset = _accountAssetsCacheService.GetTradingInstrument(order.TradingConditionId,
                    order.Instrument);

                _validateOrderService.ValidateOrderStops(order.GetOrderDirection(), quote,
                    accountAsset.Delta, accountAsset.Delta, tp, sl, expOpenPrice, order.AssetAccuracy);

                order.TakeProfit = tp.HasValue ? Math.Round(tp.Value, order.AssetAccuracy) : (decimal?)null;
                order.StopLoss = sl.HasValue ? Math.Round(sl.Value, order.AssetAccuracy) : (decimal?)null;
                order.ExpectedOpenPrice = expOpenPrice.HasValue ? Math.Round(expOpenPrice.Value, order.AssetAccuracy) : (decimal?)null;
                order.LastModified = DateTime.UtcNow;
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

        private void ProcessOrdersClosing(string instrument = null)
        {
            var closingOrders = string.IsNullOrEmpty(instrument)
                ? _ordersCache.ClosingOrders.GetAllOrders()
                : _ordersCache.ClosingOrders.GetOrdersByInstrument(instrument);

            if (closingOrders.Count == 0)
                return;

            foreach (var order in closingOrders)
            {
                var me = _meRouter.GetMatchingEngineForClose(order);

                ProcessOrdersClosingByMatchingEngine(order, me);
            }
        }

        private void ProcessOrdersClosingByMatchingEngine(Position order, IMatchingEngineBase matchingEngine)
        {
            order.CloseOrderbookId = matchingEngine.Id;

            matchingEngine.MatchMarketOrderForCloseAsync(order, matchedOrders =>
            {
                if (matchedOrders.Count == 0)
                {
                    //if order did not started to close yet and for any reason we did not close it now - make active
                    if (!order.MatchedCloseOrders.Any())
                    {
                        order.Status = PositionStatus.Active;
                        order.CloseRejectReasonText = "No orders to match";

                        _ordersCache.ActiveOrders.Add(order);
                        _ordersCache.ClosingOrders.Remove(order);
                    }

                    return false;
                }

                MakeOrderClosed(order, matchedOrders);

                return true;
            }).GetAwaiter().GetResult();
        }

        private void MakeOrderClosed(Position order, MatchedOrderCollection matchedOrders)
        {
            order.MatchedCloseOrders.AddRange(matchedOrders);

            if (order.GetIsCloseFullfilled())
            {
                order.Status = PositionStatus.Closed;
                order.CloseDate = DateTime.UtcNow;
                _ordersCache.ClosingOrders.Remove(order);
                _orderClosedEventChannel.SendEvent(this, new OrderClosedEventArgs(order));
            }
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            ProcessOrdersClosing(ea.BidAskPair.Instrument);
            ProcessOrdersActive(ea.BidAskPair.Instrument);
            ProcessOrdersWaitingForExecution(ea.BidAskPair.Instrument);
        }
    }
}
