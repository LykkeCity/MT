﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Common;
using MarginTrading.Core;
using MarginTrading.Core.Exceptions;
using MarginTrading.Services.Events;
using MarginTradingHelpers = MarginTrading.Services.Helpers.MarginTradingHelpers;

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
        private readonly ISwapCommissionService _swapCommissionService;
        private readonly IValidateOrderService _validateOrderService;
        private readonly IRabbitMqNotifyService _rabbitMqNotifyService;
        private readonly IClientNotifyService _notifyService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IAggregatedOrderBook _aggregatedOrderBook;
        private readonly OrdersCache _ordersCache;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly IMatchingEngineRouter _meRouter;
        private readonly IThreadSwitcher _threadSwitcher;
        private readonly IMatchingEngineRepository _meRepository;

        public TradingEngine(
            IEventChannel<MarginCallEventArgs> marginCallEventChannel,
            IEventChannel<StopOutEventArgs> stopoutEventChannel,
            IEventChannel<OrderPlacedEventArgs> orderPlacedEventChannel,
            IEventChannel<OrderClosedEventArgs> orderClosedEventChannel,
            IEventChannel<OrderCancelledEventArgs> orderCancelledEventChannel,

            IValidateOrderService validateOrderService,
            IQuoteCacheService quoteCashService,
            IAccountUpdateService accountUpdateService,
            ISwapCommissionService swapCommissionService,
            IClientNotifyService notifyService,
            IRabbitMqNotifyService rabbitMqNotifyService,
            IAccountsCacheService accountsCacheService,
            IAggregatedOrderBook aggregatedOrderBook,
            OrdersCache ordersCache,
            IAccountAssetsCacheService accountAssetsCacheService,
            IMatchingEngineRouter meRouter,
            IThreadSwitcher threadSwitcher,
            IMatchingEngineRepository meRepository)
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
            _aggregatedOrderBook = aggregatedOrderBook;
            _ordersCache = ordersCache;
            _accountAssetsCacheService = accountAssetsCacheService;
            _notifyService = notifyService;
            _meRouter = meRouter;
            _threadSwitcher = threadSwitcher;
            _meRepository = meRepository;
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

        private Order PlaceMarketOrderByMatchingEngine(Order order, IMatchingEngine matchingEngine)
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

                if (matchedOrders.GetTotalVolume() < Math.Abs(order.Volume) && order.FillType == OrderFillType.FillOrKill)
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

                MakeOrderAcitve(order, matchedOrders);

                return true;
            });

            if (order.Status == OrderStatus.Rejected)
            {
                _rabbitMqNotifyService.OrdeReject(order);
            }

            return order;
        }

        private async Task<Order> PlaceMarketOrderByMatchingEngineProxy(Order order, IMatchingEngineProxy meProxy)
        {
            var matchedOrders = await meProxy.GetMatchedOrdersForOpenAsync(order);

            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                MakeOrderAcitve(order, matchedOrders);
            }

            return order;
        }

        private void RejectOrder(Order order, OrderRejectReason reason, string message, string comment = null)
        {
            order.CloseDate = DateTime.UtcNow;
            order.Status = OrderStatus.Rejected;
            order.RejectReason = reason;
            order.RejectReasonText = message;
            order.Comment = comment;

            _rabbitMqNotifyService.OrdeReject(order);
        }

        private async Task<Order> PlaceOrderByMarketPrice(Order order)
        {
            try
            {
                var me = _meRouter.GetMatchingEngine(order.ClientId, order.TradingConditionId, order.Instrument, order.GetOrderType());

                var meBase = me as IMatchingEngineBase;

                if (meBase == null)
                    throw new Exception("Orderbook not found");

                order.OpenOrderbookId = meBase.Id;

                var matchingEngine = meBase as IMatchingEngine;

                if (matchingEngine != null)
                    return PlaceMarketOrderByMatchingEngine(order, matchingEngine);

                var orderbookProxy = meBase as IMatchingEngineProxy;

                if (orderbookProxy != null)
                    return await PlaceMarketOrderByMatchingEngineProxy(order, orderbookProxy);

                throw new Exception("Orderbook not found");
            }
            catch (QuoteNotFoundException ex)
            {
                RejectOrder(order, OrderRejectReason.NoLiquidity, ex.Message);
                return order;
            }
            catch (Exception ex)
            {
                RejectOrder(order, OrderRejectReason.TechnicalError, ex.Message);
                return order;
            }
        }

        private void MakeOrderAcitve(Order order, MatchedOrder[] matchedOrders)
        {
            order.MatchedOrders.AddRange(matchedOrders);
            order.OpenPrice = Math.Round(order.MatchedOrders.GetWeightedAveragePrice(), order.AssetAccuracy);
            order.OpenDate = DateTime.UtcNow;
            order.Status = OrderStatus.Active;

            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            _swapCommissionService.SetCommissions(account.TradingConditionId, account.BaseAssetId, order);
            _ordersCache.ActiveOrders.Add(order);
            _orderPlacedEventChannel.SendEvent(this, new OrderPlacedEventArgs(order));
        }

        //TODO: do check in other way
        private void CheckIfWeCanOpenPosition(Order order, MatchedOrder[] matchedOrders)
        {
            var accountAsset = _accountAssetsCacheService.GetAccountAsset(order.TradingConditionId, order.AccountAssetId, order.Instrument);
            _validateOrderService.ValidateInstrumentPositionVolume(accountAsset, order);

            order.MatchedOrders = matchedOrders.ToList();
            order.OpenPrice = Math.Round(order.MatchedOrders.GetWeightedAveragePrice(), order.AssetAccuracy);
            
            InstrumentBidAskPair quote;
            if (_quoteCashService.TryGetQuoteById(order.Instrument, out quote))
            {
                order.ClosePrice = order.GetOrderType() == OrderDirection.Buy ? quote.Bid : quote.Ask;
            }

            var guessAccount = _accountUpdateService.GuessAccountWithOrder(order);
            var guessAccountLevel = guessAccount.GetAccountLevel();

            order.OpenPrice = 0;
            order.ClosePrice = 0;
            order.MatchedOrders = new List<MatchedOrder>();

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
            lock (MarginTradingHelpers.TradingMatchingSync)
                _ordersCache.WaitingForExecutionOrders.Add(order);

            _orderPlacedEventChannel.SendEvent(this, new OrderPlacedEventArgs(order));
        }

        #region Orders waition for execution
        private void ProcessOrdersWaitingForExecution(string instrument)
        {
            var orders = GetPendingOrdersToBeExecuted(instrument).ToArray();

            if (orders.Length == 0)
                return;

            lock (MarginTradingHelpers.TradingMatchingSync)
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
            var pendingOrders = _ordersCache.WaitingForExecutionOrders.GetOrders(instrument)
                .OrderBy(item => item.CreateDate);

            foreach (var order in pendingOrders)
            {
                var price = _aggregatedOrderBook.GetPriceFor(order.Instrument, order.GetCloseType());

                if (price.HasValue && order.IsSuitablePriceForPendingOrder(price.Value))
                    yield return order;
            }
        }

        #endregion

        #region Active orders

        private void ProcessOrdersActive(string instrument)
        {
            var stopoutAccounts = UpdateClosePriceAndDetectStopout(instrument).ToArray();
            foreach (var tuple in stopoutAccounts)
                CommitStopout(tuple.Item1, tuple.Item2);

            foreach (var order in _ordersCache.ActiveOrders.GetOrders(instrument))
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
            var openOrders = _ordersCache.ActiveOrders.GetOrders(instrument).GroupBy(x => x.AccountId).ToDictionary(x => x.Key, x => x.ToArray());

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
                        if (matchedOrders.Length == 0)
                            return false;

                        order.UpdateClosePrice(Math.Round(matchedOrders.ToList().GetWeightedAveragePrice(), order.AssetAccuracy));
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
            foreach (var order in orders)
                SetOrderToClosingState(order, OrderCloseReason.StopOut);

            //TODO: figure out when we send this event - when we here or when we closed orders
            _stopoutEventChannel.SendEvent(this, new StopOutEventArgs(account, orders));
        }

        private void SetOrderToClosingState(Order order, OrderCloseReason reason)
        {
            order.Status = OrderStatus.Closing;
            order.StartClosingDate = DateTime.UtcNow;
            order.CloseReason = reason;

            if (string.IsNullOrEmpty(order.CloseOrderbookId))
            {
                var me = _meRouter.GetMatchingEngine(order.ClientId, order.TradingConditionId, order.Instrument, order.GetCloseType());

                var meBase = me as IMatchingEngineBase;

                if (meBase == null)
                    throw new Exception("Orderbook not found");

                order.CloseOrderbookId = meBase.Id;
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

        private Order CloseActiveOrderByMatchingEngine(Order order, OrderCloseReason reason, IMatchingEngine matchingEngine)
        {
            order.StartClosingDate = DateTime.UtcNow;
            order.CloseReason = reason;

            matchingEngine.MatchMarketOrderForClose(order, matchedOrders =>
            {
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

            return order;
        }

        private async Task<Order> CloseActiveOrderByMatchingEngineProxyAsync(Order order, OrderCloseReason reason, IMatchingEngineProxy orderbookProxy)
        {
            order.StartClosingDate = DateTime.UtcNow;
            order.CloseReason = reason;

            var matchedOrders = await orderbookProxy.GetMatchedOrdersForCloseAsync(order);

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

            return order;
        }

        public async Task<Order> CloseActiveOrderAsync(string orderId, OrderCloseReason reason)
        {
            var order = GetActiveOrderForClose(orderId);

            var me = _meRouter.GetMatchingEngine(order.ClientId, order.TradingConditionId, order.Instrument, order.GetCloseType());

            var meBase = me as IMatchingEngineBase;

            if (meBase == null)
                throw new Exception("Orderbook not found");

            order.CloseOrderbookId = meBase.Id;

            var matchingEngine = meBase as IMatchingEngine;

            if (matchingEngine != null)
                return CloseActiveOrderByMatchingEngine(order, reason, matchingEngine);

            var meProxy = meBase as IMatchingEngineProxy;

            if (meProxy != null)
                return await CloseActiveOrderByMatchingEngineProxyAsync(order, reason, meProxy);

            throw new Exception("Orderbook not found");
        }

        public Order CancelPendingOrder(string orderId, OrderCloseReason reason)
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                var order = _ordersCache.WaitingForExecutionOrders.GetOrderById(orderId);
                CancelWaitingForExecutionOrder(order, reason);
                return order;
            }
        }
        #endregion


        private Order GetActiveOrderForClose(string orderId)
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
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

        public void ChangeOrderLimits(string orderId, double stopLoss, double takeProfit, double expectedOpenPrice = 0)
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                var order = _ordersCache.GetOrderById(orderId);

                if (order.Status != OrderStatus.WaitingForExecution && expectedOpenPrice > 0)
                {
                    return;
                }

                var quote = _quoteCashService.GetQuote(order.Instrument);
                double? tp = takeProfit == 0 ? (double?)null : takeProfit;
                double? sl = stopLoss == 0 ? (double?)null : stopLoss;
                double? expOpenPrice = expectedOpenPrice == 0 ? (double?)null : expectedOpenPrice;

                var accountAsset = _accountAssetsCacheService.GetAccountAsset(order.TradingConditionId,
                    order.AccountAssetId, order.Instrument);

                _validateOrderService.ValidateOrderStops(order.GetOrderType(), quote,
                    accountAsset.DeltaBid, accountAsset.DeltaAsk, tp, sl, expOpenPrice, order.AssetAccuracy);

                order.TakeProfit = tp.HasValue ? Math.Round(tp.Value, order.AssetAccuracy) : (double?)null;
                order.StopLoss = sl.HasValue ? Math.Round(sl.Value, order.AssetAccuracy) : (double?)null;
                order.ExpectedOpenPrice = expOpenPrice.HasValue ? Math.Round(expOpenPrice.Value, order.AssetAccuracy) : (double?)null;
                _notifyService.NotifyOrderChanged(order);
            }
        }

        public bool PingLock()
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                return true;
            }
        }

        // TODO: Resolve situalion when we have no liquidity!
        private void ProcessOrdersClosing(string instrument)
        {
            var closingOrders = _ordersCache.ClosingOrders.GetOrders(instrument).ToArray();

            if (closingOrders.Length == 0)
                return;

            var meProxyList = new List<Tuple<Order, IMatchingEngineProxy>>();

            foreach (var order in closingOrders)
            {
                var meBase = _meRepository.GetMatchingEngineById(order.CloseOrderbookId);
                var me = meBase as IMatchingEngine;

                if (me != null)
                {
                    ProcessOrdersClosingByMatchingEngine(order, me);
                    continue;
                }

                var meProxy = meBase as IMatchingEngineProxy;

                if (meProxy != null)
                {
                    meProxyList.Add(new Tuple<Order, IMatchingEngineProxy>(order, meProxy));
                }
            }

            if (meProxyList.Count > 0)
            {
                _threadSwitcher.SwitchThread(async () =>
                {
                    foreach (var tuple in meProxyList)
                        await ProcessOrdersClosingByMatchingEngineProxyAsync(tuple.Item1, tuple.Item2);
                });
            }
        }

        //TODO: check if order can't be closed from different thread simultaniously
        private async Task ProcessOrdersClosingByMatchingEngineProxyAsync(Order order, IMatchingEngineProxy meProxy)
        {
            var matchedOrders = await meProxy.GetMatchedOrdersForCloseAsync(order);

            if (matchedOrders.Length == 0)
                return;

            lock (MarginTradingHelpers.TradingMatchingSync)
                MakeOrderClosed(order, matchedOrders);
        }

        private void ProcessOrdersClosingByMatchingEngine(Order order, IMatchingEngine matchingEngine)
        {
            matchingEngine.MatchMarketOrderForClose(order, matchedOrders =>
            {
                if (matchedOrders.Length == 0)
                    return false;

                MakeOrderClosed(order, matchedOrders);

                return true;
            });
        }

        private void MakeOrderClosed(Order order, MatchedOrder[] matchedOrders)
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
