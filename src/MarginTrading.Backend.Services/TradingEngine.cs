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
        private readonly IEventChannel<OrderExecutedEventArgs> _orderExecutedEventChannel;
        private readonly IEventChannel<OrderCancelledEventArgs> _orderCancelledEventChannel;
        private readonly IEventChannel<OrderChangedEventArgs> _orderLimitsChangesEventChannel;
        private readonly IEventChannel<OrderExecutionStartedEventArgs> _orderClosingEventChannel;
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
        private readonly ICfdCalculatorService _cfdCalculatorService;

        public TradingEngine(
            IEventChannel<MarginCallEventArgs> marginCallEventChannel,
            IEventChannel<StopOutEventArgs> stopoutEventChannel,
            IEventChannel<OrderPlacedEventArgs> orderPlacedEventChannel,
            IEventChannel<OrderExecutedEventArgs> orderClosedEventChannel,
            IEventChannel<OrderCancelledEventArgs> orderCancelledEventChannel, 
            IEventChannel<OrderChangedEventArgs> orderLimitsChangesEventChannel,
            IEventChannel<OrderExecutionStartedEventArgs> orderClosingEventChannel,
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
            IDateService dateService,
            ICfdCalculatorService cfdCalculatorService)
        {
            _marginCallEventChannel = marginCallEventChannel;
            _stopoutEventChannel = stopoutEventChannel;
            _orderPlacedEventChannel = orderPlacedEventChannel;
            _orderExecutedEventChannel = orderClosedEventChannel;
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
            _cfdCalculatorService = cfdCalculatorService;
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
                RejectOrder(order, ex.RejectReason, ex.Message, ex.Comment);
                return order;
            }
        }

        private async Task<Order> PlaceMarketOrderByMatchingEngineAsync(Order order, IMatchingEngineBase matchingEngine)
        {
            order.SetMatchingEngine(matchingEngine.Id);

            var equivalentRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.EquivalentAsset,
                order.AssetPairId, order.LegalEntity);
            var fxRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.AccountAssetId,
                order.AssetPairId, order.LegalEntity);

            order.SetRates(equivalentRate, fxRate);
            
            await matchingEngine.MatchMarketOrderForOpenAsync(order, matchedOrders =>
            {
                if (!matchedOrders.Any())
                {
                    order.Reject(OrderRejectReason.NoLiquidity, "No orders to match", "", _dateService.Now());
                    return false;
                }

                if (matchedOrders.SummaryVolume < Math.Abs(order.Volume) && order.FillType == OrderFillType.FillOrKill)
                {
                    order.Reject(OrderRejectReason.NoLiquidity, "Not fully matched", "", _dateService.Now());
                    return false;
                }

                try
                {
                    //TODO: make this validation Pre-Trade
                    //CheckIfWeCanOpenPosition(order, matchedOrders);
                }
                catch (ValidateOrderException e)
                {
//                    order.CloseDate = DateTime.UtcNow;
//                    order.Status = PositionStatus.Rejected;
//                    order.RejectReason = e.RejectReason;
//                    order.RejectReasonText = e.Message;
//                    return false;
                }

                order.Execute(_dateService.Now(), matchedOrders);

                return true;
            });

            if (order.Status == OrderStatus.Rejected)
            {
                _orderRejectedEventChannel.SendEvent(this, new OrderRejectedEventArgs(order));
            }
            else
            {
                _orderExecutedEventChannel.SendEvent(this, new OrderExecutedEventArgs(order));
            }

            if (order.OrderType != OrderType.Market)
            {
                _ordersCache.Active.Remove(order);
            }
            
            return order;
        }

        private void RejectOrder(Order order, OrderRejectReason reason, string message, string comment = null)
        {
            order.Reject(reason, message, comment, _dateService.Now());
            _orderRejectedEventChannel.SendEvent(this, new OrderRejectedEventArgs(order));
        }

        private Task<Order> PlaceOrderByMarketPrice(Order order)
        {
            try
            {
                var me = _meRouter.GetMatchingEngineForExecution(order);

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

        private void CheckIfWeCanOpenPosition(Position order, MatchedOrderCollection matchedOrders)
        {
            var accountAsset =
                _accountAssetsCacheService.GetTradingInstrument(order.TradingConditionId, order.AssetPairId);
            _validateOrderService.ValidateInstrumentPositionVolume(accountAsset, order);

            //order.MatchedOrders.AddRange(matchedOrders);
            //order.OpenPrice = Math.Round(order.MatchedOrders.WeightedAveragePrice, order.AssetPairAccuracy);

            var defaultMatchingEngine = _meRouter.GetMatchingEngineForClose(order);

            var closePrice = defaultMatchingEngine.GetPriceForClose(order);

            if (!closePrice.HasValue)
            {
                throw new ValidateOrderException(OrderRejectReason.NoLiquidity, "No orders to match for close");
            }
            
            order.UpdateClosePrice(Math.Round(closePrice.Value, order.AssetPairAccuracy));

            //TODO: very strange check.. think about it one more time
            var guessAccount = _accountUpdateService.GuessAccountWithNewActiveOrder(order);
            var guessAccountLevel = guessAccount.GetAccountLevel();

            if (guessAccountLevel != AccountLevel.None)
            {
                //order.OpenPrice = 0;
                //order.ClosePrice = 0;
                //order.MatchedOrders = new MatchedOrderCollection();
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
            var me = _meRouter.GetMatchingEngineForExecution(order);
            order.SetMatchingEngine(me.Id);

            if (order.Status == OrderStatus.Inactive)
            {
                _ordersCache.Inactive.Add(order);
            }
            else if (order.Status == OrderStatus.Active)
            {
                _ordersCache.Active.Add(order);
            }
            else
            {
                throw new ValidateOrderException(OrderRejectReason.TechnicalError,
                    $"Invalid order status: {order.Status}");
            }
                
            _orderPlacedEventChannel.SendEvent(this, new OrderPlacedEventArgs(order));
        }

        #region Orders waiting for execution
        
        private void ProcessOrdersWaitingForExecution(string instrument)
        {
            //ProcessPendingOrdersMarginRecalc(instrument);
            
            var orders = GetPendingOrdersToBeExecuted(instrument).ToArray();

            if (orders.Length == 0)
                return;

            //using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(ProcessOrdersWaitingForExecution)}"))
            //{
                foreach (var order in orders)
                    _ordersCache.Active.Remove(order);
            //}

            //TODO: think how to make sure that we don't loose orders
            _threadSwitcher.SwitchThread(async () =>
            {
                //TODO: implement
                foreach (var order in orders)
                    await PlaceOrderByMarketPrice(order);
            });

        }

        private IEnumerable<Order> GetPendingOrdersToBeExecuted(string instrument)
        {
            var pendingOrders = _ordersCache.Active.GetOrdersByInstrument(instrument)
                .OrderBy(item => item.Created);

            foreach (var order in pendingOrders)
            {
                InstrumentBidAskPair pair;

                if (_quoteCashService.TryGetQuoteById(order.AssetPairId, out pair))
                {
                    var price = pair.GetPriceForOrderType(order.Direction);

                    if (order.IsSuitablePriceForPendingOrder(price) &&
                        !_assetPairDayOffService.ArePendingOrdersDisabled(order.AssetPairId))
                        yield return order;
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

        #region Active orders

        private void ProcessOrdersActive(string instrument)
        {
            var stopoutAccounts = UpdateClosePriceAndDetectStopout(instrument).ToArray();
            foreach (var account in stopoutAccounts)
                CommitStopout(account);

            foreach (var order in _ordersCache.Positions.GetOrdersByInstrument(instrument))
            {
                //if (order.IsStopLoss())
                //    SetOrderToClosingState(order, OrderCloseReason.StopLoss);
                //else if (order.IsTakeProfit())
                //    SetOrderToClosingState(order, OrderCloseReason.TakeProfit);
            }

            ProcessInProgressOrders(instrument);
        }

        private IEnumerable<MarginTradingAccount> UpdateClosePriceAndDetectStopout(string instrument)
        {
            var openOrders = _ordersCache.Positions.GetOrdersByInstrument(instrument)
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
                        order.UpdateClosePrice(Math.Round(closePrice.Value, order.AssetPairAccuracy));
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
            var pendingOrders = _ordersCache.Active.GetOrdersByAccountIds(account.Id);

            var cancelledPendingOrders = new List<Order>();
            
            foreach (var pendingOrder in pendingOrders)
            {
                cancelledPendingOrders.Add(pendingOrder);
                CancelPendingOrder(pendingOrder.Id, PositionCloseReason.CanceledBySystem, "Stop out");
            }
            
            var activeOrders = _ordersCache.Positions.GetOrdersByAccountIds(account.Id);
            
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
                new StopOutEventArgs(account, ordersToClose/*.Concat(cancelledPendingOrders)*/.ToArray()));

            foreach (var order in ordersToClose)
                SetOrderToClosingState(order, PositionCloseReason.StopOut);
        }

        private void SetOrderToClosingState(Position position, PositionCloseReason reason)
        {
            position.StartClosing(_dateService.Now(), reason, OriginatorType.Investor, "");
            
            //_ordersCache.ClosingOrders.Add(order);
            //_ordersCache.Positions.Remove(order);
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

        public Task<Order> ClosePositionAsync(string positionId, PositionCloseReason reason, string comment = null)
        {
            var position = _ordersCache.Positions.GetOrderById(positionId);

            var me = _meRouter.GetMatchingEngineForClose(position);

            Order closeOrder = null;

            return PlaceMarketOrderByMatchingEngineAsync(closeOrder, me /*, reason, comment*/);
        }

        public Order CancelPendingOrder(string orderId, PositionCloseReason reason, string comment = null)
        {
            using (_contextFactory.GetWriteSyncContext($"{nameof(TradingEngine)}.{nameof(CancelPendingOrder)}"))
            {
                var order = _ordersCache.Active.GetOrderById(orderId);
                CancelWaitingForExecutionOrder(order, reason, comment);
                return order;
            }
        }
        #endregion


        private void CancelWaitingForExecutionOrder(Order order, PositionCloseReason reason, string comment)
        {
//            order.Status = PositionStatus.Closed;
//            order.CloseDate = DateTime.UtcNow;
//            order.CloseReason = reason;
//            order.Comment = comment;
            
            _ordersCache.Active.Remove(order);

            _orderCancelledEventChannel.SendEvent(this, new OrderCancelledEventArgs(order));
        }

        public void ChangeOrderLimits(string orderId, decimal price)
        {
            var order = _ordersCache.GetOrderById(orderId);

            order.ChangePrice(price, _dateService.Now());

            _orderLimitsChangesEventChannel.SendEvent(this, new OrderChangedEventArgs(order));
        }

        public bool PingLock()
        {
            using (_contextFactory.GetReadSyncContext($"{nameof(TradingEngine)}.{nameof(PingLock)}"))
            {
                return true;
            }
        }

        private void ProcessInProgressOrders(string instrument = null)
        {
            var orders = string.IsNullOrEmpty(instrument)
                ? _ordersCache.InProgress.GetAllOrders()
                : _ordersCache.InProgress.GetOrdersByInstrument(instrument);

            if (orders.Count == 0)
                return;

            foreach (var order in orders)
            {
                var me = _meRouter.GetMatchingEngineForExecution(order);

                PlaceMarketOrderByMatchingEngineAsync(order, me);
            }
        }

        int IEventConsumer.ConsumerRank => 100;

        void IEventConsumer<BestPriceChangeEventArgs>.ConsumeEvent(object sender, BestPriceChangeEventArgs ea)
        {
            ProcessInProgressOrders(ea.BidAskPair.Instrument);
            ProcessOrdersActive(ea.BidAskPair.Instrument);
            ProcessOrdersWaitingForExecution(ea.BidAskPair.Instrument);
        }
    }
}
