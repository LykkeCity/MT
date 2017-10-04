using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Core.MatchedOrders;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class MatchingEngineTests : BaseTests
    {
        private IQuoteCacheService _quoteCashService;
        private IMatchingEngine _matchingEngine;
        private string _marketMakerId1;
        private IAggregatedOrderBook _aggregatedOrderBook;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RegisterDependencies();
            _marketMakerId1 = "1";

            _quoteCashService = Container.Resolve<IQuoteCacheService>();
            _matchingEngine = Container.Resolve<IMatchingEngine>();
            _aggregatedOrderBook = Container.Resolve<IAggregatedOrderBook>();

        }

        [SetUp]
        public void SetUp()
        {
            var ordersSet = new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "1", Instrument = "EURUSD", MarketMakerId = _marketMakerId1, Price = 1.04M, Volume = 4 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "2", Instrument = "EURUSD", MarketMakerId = _marketMakerId1, Price = 1.05M, Volume = 7 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "3", Instrument = "EURUSD", MarketMakerId = _marketMakerId1, Price = 1.1M, Volume = -6 },
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "4", Instrument = "EURUSD", MarketMakerId = _marketMakerId1, Price = 1.15M, Volume = -8 }
            };

            _matchingEngine.SetOrders(_marketMakerId1, ordersSet);
        }

        [TearDown]
        public void TeadDown()
        {
            _matchingEngine.SetOrders(_marketMakerId1, null, null, true);
        }

        [Test]
        public void Check_Default_Order_Status()
        {
            var order = new Order();
            Assert.AreEqual(OrderStatus.WaitingForExecution, order.Status);
        }

        [Test]
        public void Check_Default_Order_Fill_Type()
        {
            var order = new Order();
            Assert.AreEqual(OrderFillType.FillOrKill, order.FillType);
        }

        [Test]
        public void Check_Default_Reject_Reason()
        {
            var order = new Order();
            Assert.AreEqual(OrderRejectReason.None, order.RejectReason);
        }

        [Test]
        public void Check_Default_Close_Reason()
        {
            var order = new Order();
            Assert.AreEqual(OrderCloseReason.None, order.CloseReason);
        }

        [Test]
        public void Check_Order_Is_Buy()
        {
            var order = new Order {Volume = 10};
            Assert.AreEqual(OrderDirection.Buy, order.GetOrderType());
        }

        [Test]
        public void Check_Order_Is_Sell()
        {
            var order = new Order {Volume = -10};
            Assert.AreEqual(OrderDirection.Sell, order.GetOrderType());
        }

        [Test]
        public void Is_Orders_Added_To_OrderBook()
        {
            const string instrument = "EURUSD";

            var buyLevels = _aggregatedOrderBook.GetBuy(instrument);
            var sellLevels = _aggregatedOrderBook.GetSell(instrument);
            var quote = _quoteCashService.GetQuote(instrument);

            Assert.AreEqual(2, buyLevels.Count);
            Assert.AreEqual(2, sellLevels.Count);

            Assert.AreEqual(1.05, quote.Bid);
            Assert.AreEqual(1.1, quote.Ask);
        }

        [Test]
        public void Is_Orders_Replaced_In_OrderBook()
        {
            const string instrument = "EURUSD";

            _matchingEngine.SetOrders(_marketMakerId1, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = instrument, MarketMakerId = "1", Price = 1.15M, Volume = -4 } //should replace volume for order id = 4 from -8 to -4
            });

            var sellLevels = _aggregatedOrderBook.GetSell(instrument);
            var quote = _quoteCashService.GetQuote(instrument);

            Assert.AreEqual(2, sellLevels.Count);
            Assert.AreEqual(1, sellLevels.Count(item => item.Price == 1.15M));
            Assert.AreEqual(-4, sellLevels.First(item => item.Price == 1.15M).Volume); //replaced volume

            Assert.AreEqual(1.05, quote.Bid);
            Assert.AreEqual(1.1, quote.Ask);
        }

        [Test]
        public void Is_Selected_Orders_Deleted_From_OrderBook()
        {
            _matchingEngine.SetOrders(_marketMakerId1, new LimitOrder[] {}, new[] { "2", "3"}); //delete EURUSD buy 15 @ 1.05 and EURUSD sell 10 @ 1.1 orders

            const string instrument = "EURUSD";

            var buyLevels = _aggregatedOrderBook.GetBuy(instrument);
            var sellLevels = _aggregatedOrderBook.GetSell(instrument);
            var quote = _quoteCashService.GetQuote(instrument);

            Assert.AreEqual(1, buyLevels.Count);
            Assert.AreEqual(1, sellLevels.Count);

            Assert.AreEqual(1.04, quote.Bid);
            Assert.AreEqual(1.15, quote.Ask);
        }

        [Test]
        public void Is_All_Orders_Deleted_From_OrderBook()
        {
            _matchingEngine.SetOrders(_marketMakerId1, null, null, true);

            const string instrument = "EURUSD";

            var buyLevels = _aggregatedOrderBook.GetBuy(instrument);
            var sellLevels = _aggregatedOrderBook.GetSell(instrument);

            Assert.AreEqual(0, buyLevels.Count);
            Assert.AreEqual(0, sellLevels.Count);
        }

        [Test]
        public void Is_Aggregated_OrderBook_Correct()
        {
            const string instrument = "EURUSD";

            var buyLevels = _aggregatedOrderBook.GetBuy(instrument);
            var sellLevels = _aggregatedOrderBook.GetSell(instrument);

            Assert.AreEqual(2, buyLevels.Count);
            Assert.AreEqual(2, sellLevels.Count);
            Assert.IsTrue(sellLevels.Any(item => item.Price == 1.15M && item.Volume == -8));
            Assert.IsTrue(sellLevels.Any(item => item.Price == 1.1M && item.Volume == -6));
            Assert.IsTrue(buyLevels.Any(item => item.Price == 1.04M && item.Volume == 4));
            Assert.IsTrue(buyLevels.Any(item => item.Price == 1.05M && item.Volume == 7));

            _matchingEngine.SetOrders(_marketMakerId1, new[]
            {
                new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = "1", Price = 1.15M, Volume = -4}
            });

            buyLevels = _aggregatedOrderBook.GetBuy(instrument);
            sellLevels = _aggregatedOrderBook.GetSell(instrument);

            Assert.AreEqual(2, buyLevels.Count);
            Assert.AreEqual(2, sellLevels.Count);
            Assert.IsTrue(sellLevels.Any(item => item.Price == 1.15M && item.Volume == -4));
        }

        [Test]
        public void Is_Sell_Orders_Deleted_By_Instrument()
        {
            _matchingEngine.SetOrders(new SetOrderModel
            {
                MarketMakerId = _marketMakerId1,
                OrdersToAdd = new []
                {
                    new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = "1", Price = 1.16M, Volume = -10}
                },
                DeleteByInstrumentsSell = new[] {"EURUSD"}
            });

           var orderBook = _matchingEngine.GetOrderBook(new List<string> {_marketMakerId1});

            Assert.AreEqual(1, orderBook["EURUSD"].Sell.Count);
            Assert.AreEqual(1, orderBook["EURUSD"].Sell.Count);
            Assert.AreEqual(1, orderBook["EURUSD"].Sell.First().Value.Count);
        }

        [Test]
        public void Is_Buy_Orders_Deleted_By_Instrument()
        {
            _matchingEngine.SetOrders(new SetOrderModel
            {
                MarketMakerId = _marketMakerId1,
                OrdersToAdd = new []
                {
                    new LimitOrder { CreateDate = DateTime.UtcNow, Id = "5", Instrument = "EURUSD", MarketMakerId = "1", Price = 1.07M, Volume = 10}
                },
                DeleteByInstrumentsBuy = new[] {"EURUSD"}
            });

           var orderBook = _matchingEngine.GetOrderBook(new List<string> {_marketMakerId1});

            Assert.AreEqual(1, orderBook["EURUSD"].Buy.Count);
            Assert.AreEqual(1, orderBook["EURUSD"].Buy.Count);
            Assert.AreEqual(1, orderBook["EURUSD"].Buy.First().Value.Count);
        }

        #region Market orders

        [Test]
        public void Is_PartialFill_Buy_Fully_Matched()
        {
            const string instrument = "EURUSD";

            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = instrument,
                Volume = 8,
                FillType = OrderFillType.PartialFill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(order.Volume, order.GetMatchedVolume());
            Assert.AreEqual(6, orderBooks["EURUSD"].Sell[1.15M].First(item => item.Id == "4").GetRemainingVolume());
        }

        [Test]
        public void Is_PartialFill_Sell_Fully_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = -8,
                FillType = OrderFillType.PartialFill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(Math.Abs(order.Volume), order.GetMatchedVolume());
            Assert.AreEqual(3, orderBooks["EURUSD"].Buy[1.04M].First(item => item.Id == "1").GetRemainingVolume());
            Assert.IsFalse(orderBooks["EURUSD"].Buy.ContainsKey(1.5M));
        }

        [Test]
        public void Is_PartialFill_Buy_Partial_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = 15,
                FillType = OrderFillType.PartialFill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(1, order.Volume - order.GetMatchedVolume());
            Assert.IsFalse(orderBooks["EURUSD"].Sell.ContainsKey(1.1M));
            Assert.IsFalse(orderBooks["EURUSD"].Sell.ContainsKey(1.15M));
        }

        [Test]
        public void Is_PartialFill_Sell_Partial_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = -13,
                FillType = OrderFillType.PartialFill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(2, Math.Abs(order.Volume) - order.GetMatchedVolume());
            Assert.IsFalse(orderBooks["EURUSD"].Buy.ContainsKey(1.05M));
            Assert.IsFalse(orderBooks["EURUSD"].Buy.ContainsKey(1.04M));
        }

        [Test]
        public void Is_Order_NoLiquidity_ByInstrument_Rejected()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "BTCUSD",
                Volume = 10,
                FillType = OrderFillType.PartialFill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.NoLiquidity, order.RejectReason);
            Assert.IsNotNull(order.CloseDate);
        }

        [Test]
        public void Is_FillOrKill_Buy_Rejected()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = 16,
                FillType = OrderFillType.FillOrKill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.NoLiquidity, order.RejectReason);
            Assert.IsNotNull(order.CloseDate);
            Assert.AreEqual(6, orderBooks["EURUSD"].Sell[1.1M].First(item => item.Id == "3").GetRemainingVolume());
            Assert.AreEqual(8, orderBooks["EURUSD"].Sell[1.15M].First(item => item.Id == "4").GetRemainingVolume());
        }

        [Test]
        public void Is_FillOrKill_Sell_Rejected()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = -14,
                FillType = OrderFillType.FillOrKill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(0, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Rejected, order.Status);
            Assert.AreEqual(OrderRejectReason.NoLiquidity, order.RejectReason);
            Assert.IsNotNull(order.CloseDate);
            Assert.AreEqual(4, orderBooks["EURUSD"].Buy[1.04M].First(item => item.Id == "1").GetRemainingVolume());
            Assert.AreEqual(7, orderBooks["EURUSD"].Buy[1.05M].First(item => item.Id == "2").GetRemainingVolume());
        }

        [Test]
        public void Is_FillOrKill_Buy_Fully_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = 9,
                FillType = OrderFillType.FillOrKill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(1, orderBooks["EURUSD"].Sell.Count);
            Assert.IsFalse(orderBooks["EURUSD"].Sell.ContainsKey(1.1M));
            Assert.AreEqual(5, orderBooks["EURUSD"].Sell[1.15M][0].GetRemainingVolume());
        }

        [Test]
        public void Is_FillOrKill_Sell_Fully_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = -8,
                FillType = OrderFillType.FillOrKill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(order.MatchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(1, orderBooks["EURUSD"].Buy.Count);
            Assert.IsFalse(orderBooks["EURUSD"].Buy.ContainsKey(1.05M));
            Assert.AreEqual(3, orderBooks["EURUSD"].Buy[1.04M][0].GetRemainingVolume());
        }

        #endregion

        #region Pending orders

        [Test]
        public void Is_Buy_Partial_PendingOrder_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = 8,
                ExpectedOpenPrice = 1.12M,
                FillType = OrderFillType.PartialFill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(2, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.IsFalse(orderBooks["EURUSD"].Sell.ContainsKey(1.1M));
            Assert.AreEqual(6, orderBooks["EURUSD"].Sell[1.15M][0].GetRemainingVolume());
        }

        [Test]
        public void Is_Buy_FillOrKill_PendingOrder_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = 1,
                ExpectedOpenPrice = 1.12M,
                FillType = OrderFillType.FillOrKill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(1.1, order.OpenPrice);
        }

        [Test]
        public void Is_Sell_Partial_PendingOrder_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = -5,
                ExpectedOpenPrice = 1.04M,
                FillType = OrderFillType.PartialFill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(1.05, order.OpenPrice);
            Assert.AreEqual(2, orderBooks["EURUSD"].Buy[1.05M][0].GetRemainingVolume());
        }

        [Test]
        public void Is_Sell_FillOrKill_PendingOrder_Matched()
        {
            var order = new Order
            {
                CreateDate = DateTime.UtcNow,
                Id = Guid.NewGuid().ToString("N"),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Instrument = "EURUSD",
                Volume = -5,
                ExpectedOpenPrice = 1.04M,
                FillType = OrderFillType.FillOrKill
            };

            _matchingEngine.MatchMarketOrderForOpen(order, orders => ProcessOrders(order, orders));

            var orderBooks = _matchingEngine.GetOrderBook(new List<string> { _marketMakerId1 });

            Assert.AreEqual(1, order.MatchedOrders.Count);
            Assert.AreEqual(OrderStatus.Active, order.Status);
            Assert.AreEqual(1.05, order.OpenPrice);
            Assert.AreEqual(2, orderBooks["EURUSD"].Buy[1.05M][0].GetRemainingVolume());
        }

        #endregion

        private bool ProcessOrders(Order order, MatchedOrderCollection matchedOrders)
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

            //if (!CheckIfWeCanOpenPosition(order, matchedOrders))
            //{
            //    order.Status = OrderStatus.Rejected;
            //    order.RejectReason = OrderRejectReason.AccountStopOut;
            //    order.RejectReasonText = "Opening the position will lead to account Stop Out level";
            //    return false;
            //}

            order.MatchedOrders = matchedOrders;
            order.OpenPrice = Math.Round(order.MatchedOrders.WeightedAveragePrice, order.AssetAccuracy);
            order.OpenDate = DateTime.UtcNow;
            order.Status = OrderStatus.Active;
            return true;
        }
    }
}
