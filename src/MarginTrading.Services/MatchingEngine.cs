using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;
using MarginTrading.Services.Events;
using MarginTradingHelpers = MarginTrading.Services.Helpers.MarginTradingHelpers;

namespace MarginTrading.Services
{
    public class MatchingEngine : IMatchingEngine
    {
        private readonly IEventChannel<OrderBookChangeEventArgs> _orderbookChangeEventChannel;
        private readonly OrderBookList _orderBooks;
        private long _currentMessageId;

        public MatchingEngine(
            IEventChannel<OrderBookChangeEventArgs> orderbookChangeEventChannel,
            OrderBookList orderBooks)
        {
            _orderbookChangeEventChannel = orderbookChangeEventChannel;
            _orderBooks = orderBooks;
            _currentMessageId = 0;
        }

        public string Id => MatchingEngines.Lykke;

        public void SetOrders(SetOrderModel model)
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                var changeEventArgs = new OrderBookChangeEventArgs { MessageId = _currentMessageId++ };

                if (model.DeleteByInstrumentsBuy?.Count > 0)
                {
                    var deletedOrders = _orderBooks.DeleteAllBuyOrdersByMarketMaker(model.MarketMakerId, model.DeleteByInstrumentsBuy).ToArray();
                    changeEventArgs.AddOrderBookLevelsToDelete(deletedOrders);
                }

                if (model.DeleteByInstrumentsSell?.Count > 0)
                {
                    var deletedOrders = _orderBooks.DeleteAllSellOrdersByMarketMaker(model.MarketMakerId, model.DeleteByInstrumentsSell).ToArray();
                    changeEventArgs.AddOrderBookLevelsToDelete(deletedOrders);
                }

                if (model.DeleteAllBuy || model.DeleteAllSell)
                {
                    var deletedOrders = _orderBooks.DeleteAllOrdersByMarketMaker(model.MarketMakerId, model.DeleteAllBuy, model.DeleteAllSell).ToArray();
                    changeEventArgs.AddOrderBookLevelsToDelete(deletedOrders);
                }

                if (model.OrderIdsToDelete?.Length > 0)
                {
                    var deletedOrders = _orderBooks.DeleteMarketMakerOrders(model.MarketMakerId, model.OrderIdsToDelete).ToArray();
                    changeEventArgs.AddOrderBookLevelsToDelete(deletedOrders);
                }

                if (model.OrdersToAdd?.Count > 0)
                {
                    var addedOrders = _orderBooks.AddMarketMakerOrders(model.OrdersToAdd).ToArray();
                    changeEventArgs.AddOrderBookLevels(addedOrders);
                }

                if (changeEventArgs.HasEvents())
                    _orderbookChangeEventChannel.SendEvent(this, changeEventArgs);
            }
        }

        public Dictionary<string, OrderBook> GetOrderBook(List<string> marketMakerIds)
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                var result = new Dictionary<string, OrderBook>();

                foreach (KeyValuePair<string, OrderBook> pair in _orderBooks)
                {
                    GetOrders(marketMakerIds, pair, OrderDirection.Buy, result);
                    GetOrders(marketMakerIds, pair, OrderDirection.Sell, result);
                }

                return result;
            }
        }

        public void MatchMarketOrderForOpen(Order order, Func<MatchedOrder[], bool> matchedFunc)
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                OrderDirection type = order.GetOrderType();

                var matchedOrders = _orderBooks.Match(order, type.GetOrderTypeToMatchInOrderBook(), Math.Abs(order.Volume)).ToArray();
                if (matchedFunc(matchedOrders))
                {
                    _orderBooks.Update(order, type.GetOrderTypeToMatchInOrderBook(), matchedOrders);
                    var changeEventArgs = new OrderBookChangeEventArgs { MessageId = _currentMessageId++ };
                    changeEventArgs.AddOrderBookLevelsToDeletePartial(matchedOrders.Select(item => item.CreateLimit(order.Instrument, type.GetOrderTypeToMatchInOrderBook())).ToArray());
                    _orderbookChangeEventChannel.SendEvent(this, changeEventArgs);
                }
            }
        }

        public void MatchMarketOrderForClose(Order order, Func<MatchedOrder[], bool> matchedAction)
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                OrderDirection type = order.GetCloseType();

                var matchedOrders = _orderBooks.Match(order, type.GetOrderTypeToMatchInOrderBook(), Math.Abs(order.GetRemainingCloseVolume())).ToArray();
                if (!matchedAction(matchedOrders))
                    return;

                _orderBooks.Update(order, type.GetOrderTypeToMatchInOrderBook(), matchedOrders);
                var changeEventArgs = new OrderBookChangeEventArgs { MessageId = _currentMessageId++ };
                changeEventArgs.AddOrderBookLevels(type.GetOrderTypeToMatchInOrderBook(), order.Instrument, matchedOrders);
                _orderbookChangeEventChannel.SendEvent(this, changeEventArgs);
            } // lock
        }

        public bool PingLock()
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                return true;
            }
        }

        private void GetOrders(List<string> marketMakerIds, KeyValuePair<string, OrderBook> pair, OrderDirection type, Dictionary<string, OrderBook> result)
        {
            var ordersList = type == OrderDirection.Buy ? pair.Value.Buy : pair.Value.Sell;

            foreach (KeyValuePair<double, List<LimitOrder>> orderData in ordersList)
            {
                var orders = orderData.Value.Where(item => marketMakerIds.Contains(item.MarketMakerId)).ToList();

                if (orders.Any())
                {
                    if (!result.ContainsKey(pair.Key))
                    {
                        result.Add(pair.Key, new OrderBook());
                    }

                    var resultOrderBook = type == OrderDirection.Buy ? result[pair.Key].Buy : result[pair.Key].Sell;

                    if (!resultOrderBook.ContainsKey(orderData.Key))
                    {
                        resultOrderBook.Add(orderData.Key, new List<LimitOrder>());
                    }

                    resultOrderBook[orderData.Key].AddRange(orders);
                }
            }
        }
    }
}
