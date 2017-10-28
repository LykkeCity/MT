using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Assets;
using MarginTrading.Core.Exceptions;
using MarginTrading.Core.MatchedOrders;
using MarginTrading.Core.MatchingEngines;
using MarginTrading.Services.Events;
using MarginTrading.Services.Infrastructure;

namespace MarginTrading.Services
{
    public sealed class TradingEngine : ITradingEngine, IEventConsumer<OrderBookChangeEventArgs>
    {
        private readonly IEventChannel<MarginCallEventArgs> _marginCallEventChannel;
        private readonly IEventChannel<StopOutEventArgs> _stopoutEventChannel;
        private readonly IEventChannel<OrderPlacedEventArgs> _orderPlacedEventChannel;
        private readonly IEventChannel<OrderClosedEventArgs> _orderClosedEventChannel;
        private readonly IEventChannel<OrderCancelledEventArgs> _orderCancelledEventChannel;

        private readonly IQuoteCacheService _quoteCashService;
        private readonly IAccountUpdateService _accountUpdateService;
        private readonly ICommissionService _swapCommissionService;
        private readonly IValidateOrderService _validateOrderService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IClientNotifyService _notifyService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly IMatchingEngineRouter _meRouter;
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IMatchingEngineRepository _meRepository;
        private readonly IContextFactory _contextFactory;
        private readonly IAssetPairDayOffService _assetPairDayOffService;

        public TradingEngine(
            IEventChannel<MarginCallEventArgs> marginCallEventChannel,
            IEventChannel<StopOutEventArgs> stopoutEventChannel,
            IEventChannel<OrderPlacedEventArgs> orderPlacedEventChannel,
            IEventChannel<OrderClosedEventArgs> orderClosedEventChannel,
            IEventChannel<OrderCancelledEventArgs> orderCancelledEventChannel,

            IValidateOrderService validateOrderService,
            IQuoteCacheService quoteCashService,
            IAccountUpdateService accountUpdateService,
            ICommissionService swapCommissionService,
            IClientNotifyService notifyService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IAccountAssetsCacheService accountAssetsCacheService,
            IMatchingEngineRouter meRouter,
            IThreadSwitcher threadSwitcher,
            IMatchingEngineRepository meRepository, 
            IContextFactory contextFactory,
            IAssetPairDayOffService assetPairDayOffService)
        {
            _marginCallEventChannel = marginCallEventChannel;
            _stopoutEventChannel = stopoutEventChannel;
            _orderPlacedEventChannel = orderPlacedEventChannel;
            _orderClosedEventChannel = orderClosedEventChannel;
            _orderCancelledEventChannel = orderCancelledEventChannel;

            _quoteCashService = quoteCashService;
            _accountUpdateService = accountUpdateService;
            _swapCommissionService = swapCommissionService;
            _validateOrderService = validateOrderService;
            _rabbitMqNotifyService = rabbitMqNotifyService;
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _accountAssetsCacheService = accountAssetsCacheService;
            _notifyService = notifyService;
            _meRouter = meRouter;
            _threadSwitcher = threadSwitcher;
            _meRepository = meRepository;
            _contextFactory = contextFactory;
            _assetPairDayOffService = assetPairDayOffService;
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
            matchingEngine.MatchMarketOrderForOpen(order, matchedOrders =>
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
                    order.RejectReasonText = "No orders to match or not fully matched";
                    return false;
                }

                try
                {
                    CheckIfWeCanOpenPosition(order, matchedOrders);
                }
                catch (ValidateOrderException e)
                {
                    order.CloseDate = DateTime.UtcNow;
                    order.Status = OrderStatus.Rejected;
                    order.RejectReason = e.RejectReason;
                    order.RejectReasonText = e.Message;
                    return false;
                }

                MakeOrderActive(order, matchedOrders);

                return true;
            });

            if (order.Status == OrderStatus.Rejected)
            {
                _rabbitMqNotifyService.OrderReject(order);
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

            _rabbitMqNotifyService.OrderReject(order);
        }

        private Task<Order> PlaceOrderByMarketPrice(Order order)
        {
            try
            {
                var me = _meRouter.GetMatchingEngine(order.ClientId, order.TradingConditionId, order.Instrument, order.GetOrderType());
                if (me == null)
                    throw new Exception("Orderbook not found");

                order.OpenOrderbookId = me.Id;
                order.CloseOrderbookId = me.Id;

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
                return Task.FromResult(order);
            }
        }

        private void MakeOrderActive(Order order, MatchedOrderCollection matchedOrders)
        {
            order.MatchedOrders.AddRange(matchedOrders);
            order.OpenPrice = Math.Round(order.MatchedOrders.WeightedAveragePrice, order.AssetAccuracy);
            order.OpenDate = DateTime.UtcNow;
            order.Status = OrderStatus.Active;

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            _swapCommissionService.SetCommissionRates(account.TradingConditionId, account.BaseAssetId, order);
            _ordersCache.ActiveOrders.Add(order);
            _orderPlacedEventChannel.SendEvent(this, new OrderPlacedEventArgs(order));
        }

        //TODO: do check in other way
        private void CheckIfWeCanOpenPosition(Order order, MatchedOrderCollection matchedOrders)
        {
            var accountAsset = _accountAssetsCacheService.GetAccountAsset(order.TradingConditionId, order.AccountAssetId, order.Instrument);
            _validateOrderService.ValidateInstrumentPositionVolume(accountAsset, order);

            order.MatchedOrders.AddRange(matchedOrders);
            order.OpenPrice = Math.Round(order.MatchedOrders.WeightedAveragePrice, order.AssetAccuracy);

            InstrumentBidAskPair quote;
            if (_quoteCashService.TryGetQuoteById(order.Instrument, out quote))
            {
                order.ClosePrice = order.GetOrderType() == OrderDirection.Buy ? quote.Bid : quote.Ask;
            }

            var guessAccount = _accountUpdateService.GuessAccountWithOrder(order);
            var guessAccountLevel = guessAccount.GetAccountLevel();

            order.OpenPrice = 0;
            order.ClosePrice = 0;
            order.MatchedOrders = new MatchedOrderCollection();

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
            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(PlacePendingOrder)}"))
                _ordersCache.WaitingForExecutionOrders.Add(order);

            _orderPlacedEventChannel.SendEvent(this, new OrderPlacedEventArgs(order));
        }

        #region Orders waition for execution
        
        private void ProcessOrdersWaitingForExecution(string instrument)
        {
            var orders = GetPendingOrdersToBeExecuted(instrument).ToArray();

            if (orders.Length == 0)
                return;

            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(ProcessOrdersWaitingForExecution)}"))
            {
                foreach (var order in orders)
                    _ordersCache.WaitingForExecutionOrders.Remove(order);
            }

            //TODO: think how to make sure that we don't loose orders
            _threadSwitcher.SwitchThread(async () =>
            {
                foreach (var order in orders)
                    await PlaceOrderByMarketPrice(order);
            });

        }

        private IEnumerable<Order> GetPendingOrdersToBeExecuted(string instrument)
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
                        !_assetPairDayOffService.IsPendingOrderDisabled(order.Instrument))
                        yield return order;
                }
            }
        }

        #endregion

        #region Active orders

        private void ProcessOrdersActive(string instrument)
        {
            var stopoutAccounts = UpdateClosePriceAndDetectStopout(instrument).ToArray();
            foreach (var tuple in stopoutAccounts)
                CommitStopout(tuple.Item1, tuple.Item2);

            foreach (var order in _ordersCache.ActiveOrders.GetOrdersByInstrument(instrument))
            {
                if (order.IsStopLoss())
                    SetOrderToClosingState(order, OrderCloseReason.StopLoss);
                else if (order.IsTakeProfit())
                    SetOrderToClosingState(order, OrderCloseReason.TakeProfit);
            }

            ProcessOrdersClosing(instrument);
        }

        private IEnumerable<Tuple<MarginTradingAccount, Order[]>> UpdateClosePriceAndDetectStopout(string instrument)
        {
            var openOrders = _ordersCache.ActiveOrders.GetOrdersByInstrument(instrument).GroupBy(x => x.AccountId).ToDictionary(x => x.Key, x => x.ToArray());

            foreach (var accountOrders in openOrders)
            {
                var anyOrder = accountOrders.Value.FirstOrDefault();
                if (null == anyOrder)
                    continue;

                var account = _accountsCacheService.Get(anyOrder.ClientId, anyOrder.AccountId);
                var oldAccountLevel = account.GetAccountLevel();

                var defaultMatchingEngine = _meRepository.GetDefaultMatchingEngine();

                foreach (var order in accountOrders.Value)
                {
                    defaultMatchingEngine.MatchMarketOrderForClose(order, matchedOrders =>
                    {
                        if (matchedOrders.Count == 0)
                            return false;

                        order.UpdateClosePrice(Math.Round(matchedOrders.WeightedAveragePrice, order.AssetAccuracy));
                        return false;
                    });
                }

                var newAccountLevel = account.GetAccountLevel();

                if (oldAccountLevel != newAccountLevel)
                {
                    NotifyAccountLevelChanged(account, newAccountLevel);

                    if (newAccountLevel == AccountLevel.StopOUt)
                        yield return new Tuple<MarginTradingAccount, Order[]>(account, accountOrders.Value);
                }
            }
        }

        private void CommitStopout(MarginTradingAccount account, Order[] orders)
        {
            _stopoutEventChannel.SendEvent(this, new StopOutEventArgs(account, orders));

            foreach (var order in orders)
                SetOrderToClosingState(order, OrderCloseReason.StopOut);
        }

        private void SetOrderToClosingState(Order order, OrderCloseReason reason)
        {
            order.Status = OrderStatus.Closing;
            order.StartClosingDate = DateTime.UtcNow;
            order.CloseReason = reason;

            if (string.IsNullOrEmpty(order.CloseOrderbookId))
            {
                var me = _meRouter.GetMatchingEngine(order.ClientId, order.TradingConditionId, order.Instrument, order.GetCloseType());

                if (me == null)
                    throw new Exception("Orderbook not found");

                order.CloseOrderbookId = me.Id;
            }

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

        private Task<Order> CloseActiveOrderByMatchingEngineAsync(Order order, OrderCloseReason reason, IMatchingEngineBase matchingEngine)
        {
            order.StartClosingDate = DateTime.UtcNow;
            order.CloseReason = reason;

            matchingEngine.MatchMarketOrderForClose(order, matchedOrders =>
            {
                if (!matchedOrders.Any())
                    return false;
                
                order.MatchedCloseOrders.AddRange(matchedOrders);

                if (!order.GetIsCloseFullfilled())
                {
                    order.Status = OrderStatus.Closing;
                    _ordersCache.ActiveOrders.Remove(order);
                    _ordersCache.ClosingOrders.Add(order);
                }
                else
                {
                    order.Status = OrderStatus.Closed;
                    order.CloseDate = DateTime.UtcNow;
                    _ordersCache.ActiveOrders.Remove(order);
                    _orderClosedEventChannel.SendEvent(this, new OrderClosedEventArgs(order));
                }

                return true;
            });

            return Task.FromResult(order);
        }

        public Task<Order> CloseActiveOrderAsync(string orderId, OrderCloseReason reason)
        {
            var order = GetActiveOrderForClose(orderId);
            IMatchingEngineBase me;

            if (string.IsNullOrEmpty(order.CloseOrderbookId))
            {
                me = _meRouter.GetMatchingEngine(order.ClientId, order.TradingConditionId, order.Instrument,
                    order.GetCloseType());

                if (me == null)
                    throw new Exception("Orderbook not found");

                order.CloseOrderbookId = me.Id;
            }
            else
            {
                me = _meRepository.GetMatchingEngineById(order.CloseOrderbookId);
            }

            return CloseActiveOrderByMatchingEngineAsync(order, reason, me);
        }

        public Order CancelPendingOrder(string orderId, OrderCloseReason reason)
        {
            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(CancelPendingOrder)}"))
            {
                var order = _ordersCache.WaitingForExecutionOrders.GetOrderById(orderId);
                CancelWaitingForExecutionOrder(order, reason);
                return order;
            }
        }
        #endregion


        private Order GetActiveOrderForClose(string orderId)
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(TradingEngine)}.{nameof(GetActiveOrderForClose)}"))
                return _ordersCache.ActiveOrders.GetOrderById(orderId);
        }

        private void CancelWaitingForExecutionOrder(Order order, OrderCloseReason reason)
        {
            order.Status = OrderStatus.Closed;
            order.CloseDate = DateTime.UtcNow;
            order.CloseReason = reason;

            _ordersCache.WaitingForExecutionOrders.Remove(order);

            _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(order));
        }

        public void ChangeOrderLimits(string orderId, decimal stopLoss, decimal takeProfit, decimal expectedOpenPrice = 0)
        {
            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(ChangeOrderLimits)}"))
            {
                var order = _ordersCache.GetOrderById(orderId);

                if (order.Status != OrderStatus.WaitingForExecution && expectedOpenPrice > 0)
                {
                    return;
                }

                var quote = _quoteCashService.GetQuote(order.Instrument);
                decimal? tp = takeProfit == 0 ? (decimal?)null : takeProfit;
                decimal? sl = stopLoss == 0 ? (decimal?)null : stopLoss;
                decimal? expOpenPrice = expectedOpenPrice == 0 ? (decimal?)null : expectedOpenPrice;

                var accountAsset = _accountAssetsCacheService.GetAccountAsset(order.TradingConditionId,
                    order.AccountAssetId, order.Instrument);

                _validateOrderService.ValidateOrderStops(order.GetOrderType(), quote,
                    accountAsset.DeltaBid, accountAsset.DeltaAsk, tp, sl, expOpenPrice, order.AssetAccuracy);

                order.TakeProfit = tp.HasValue ? Math.Round(tp.Value, order.AssetAccuracy) : (decimal?)null;
                order.StopLoss = sl.HasValue ? Math.Round(sl.Value, order.AssetAccuracy) : (decimal?)null;
                order.ExpectedOpenPrice = expOpenPrice.HasValue ? Math.Round(expOpenPrice.Value, order.AssetAccuracy) : (decimal?)null;
                _notifyService.NotifyOrderChanged(order);
            }
        }

        public bool PingLock()
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(TradingEngine)}.{nameof(PingLock)}"))
            {
                return true;
            }
        }

        // TODO: Resolve situalion when we have no liquidity!
        private void ProcessOrdersClosing(string instrument)
        {
            var closingOrders = _ordersCache.ClosingOrders.GetOrdersByInstrument(instrument);

            if (closingOrders.Count == 0)
                return;

            foreach (var order in closingOrders)
            {
                var me = _meRepository.GetMatchingEngineById(order.CloseOrderbookId);

                ProcessOrdersClosingByMatchingEngine(order, me);

            }
        }

        private void ProcessOrdersClosingByMatchingEngine(Order order, IMatchingEngineBase matchingEngine)
        {
            matchingEngine.MatchMarketOrderForClose(order, matchedOrders =>
            {
                if (matchedOrders.Count == 0)
                {
                    //if order did not started to close yet and for any reason we did not close it now - make active
                    if (!order.MatchedCloseOrders.Any())
                    {
                        order.Status = OrderStatus.Active;
                        order.RejectReasonText = "No orders found to match for close.";
                        
                        _ordersCache.ActiveOrders.Add(order);
                        _ordersCache.ClosingOrders.Remove(order);
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
                _ordersCache.ClosingOrders.Remove(order);
                _orderClosedEventChannel.SendEvent(this, new OrderClosedEventArgs(order));
            }
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<OrderBookChangeEventArgs>.ConsumeEvent(object sender, OrderBookChangeEventArgs ea)
        {
            foreach (string instrumentId in ea.GetChangedInstruments())
            {
                ProcessOrdersClosing(instrumentId);
                ProcessOrdersActive(instrumentId);
                ProcessOrdersWaitingForExecution(instrumentId);
            }
        }
    }
}
