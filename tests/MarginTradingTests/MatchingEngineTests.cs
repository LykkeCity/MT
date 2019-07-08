// Copyright (c) 2019 Lykke Corp.

using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTradingTests.Helpers;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class MatchingEngineTests : BaseTests
    {
        private IMarketMakerMatchingEngine _matchingEngine;
        private string _marketMakerId1;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RegisterDependencies();
            _marketMakerId1 = "1";
            _matchingEngine = Container.Resolve<IMarketMakerMatchingEngine>();

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

           var orderBook = _matchingEngine.GetOrderBook("EURUSD");

            Assert.AreEqual(1, orderBook.Sell.Count);
            Assert.AreEqual(1, orderBook.Sell.Count);
            Assert.AreEqual(1, orderBook.Sell.First().Value.Count);
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

           var orderBook = _matchingEngine.GetOrderBook("EURUSD");

            Assert.AreEqual(1, orderBook.Buy.Count);
            Assert.AreEqual(1, orderBook.Buy.Count);
            Assert.AreEqual(1, orderBook.Buy.First().Value.Count);
        }

        [Test]
        public async Task Is_Buy_Fully_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 8);

            var matchedOrders = await _matchingEngine.MatchOrderAsync(order, false);

            var orderBooks = _matchingEngine.GetOrderBook("EURUSD");

            Assert.AreEqual(2, matchedOrders.Count);
            Assert.True(matchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(matchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(order.Volume, matchedOrders.SummaryVolume);
            Assert.AreEqual(6, orderBooks.Sell[1.15M].First(item => item.Id == "4").GetRemainingVolume());
        }

        [Test]
        public async Task Is_Sell_Fully_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, -8);
            
            var matchedOrders = await _matchingEngine.MatchOrderAsync(order, false);

            var orderBooks = _matchingEngine.GetOrderBook("EURUSD");

            Assert.AreEqual(2, matchedOrders.Count);
            Assert.True(matchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(matchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(Math.Abs(order.Volume), matchedOrders.SummaryVolume);
            Assert.AreEqual(3, orderBooks.Buy[1.04M].First(item => item.Id == "1").GetRemainingVolume());
            Assert.IsFalse(orderBooks.Buy.ContainsKey(1.5M));
        }

        [Test]
        public async Task Is_Buy_Partial_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 15);
            
            var matchedOrders = await _matchingEngine.MatchOrderAsync(order, false);
            
            var orderBooks = _matchingEngine.GetOrderBook("EURUSD");

            Assert.AreEqual(2, matchedOrders.Count);
            Assert.True(matchedOrders.Any(item => item.OrderId == "3"));
            Assert.True(matchedOrders.Any(item => item.OrderId == "4"));
            Assert.AreEqual(1, order.Volume - matchedOrders.SummaryVolume);
            Assert.IsFalse(orderBooks.Sell.ContainsKey(1.1M));
            Assert.IsFalse(orderBooks.Sell.ContainsKey(1.15M));
        }

        [Test]
        public async Task Is_Sell_Partial_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, -13);
            
            var matchedOrders = await _matchingEngine.MatchOrderAsync(order, false);
            
            var orderBooks = _matchingEngine.GetOrderBook("EURUSD");

            Assert.AreEqual(2, matchedOrders.Count);
            Assert.True(matchedOrders.Any(item => item.OrderId == "1"));
            Assert.True(matchedOrders.Any(item => item.OrderId == "2"));
            Assert.AreEqual(2, Math.Abs(order.Volume) - matchedOrders.SummaryVolume);
            Assert.IsFalse(orderBooks.Buy.ContainsKey(1.05M));
            Assert.IsFalse(orderBooks.Buy.ContainsKey(1.04M));
        }

        [Test]
        public async Task Is_Order_NoLiquidity_ByInstrument_Not_Matched()
        {
            var order = TestObjectsFactory.CreateNewOrder(OrderType.Market, "BTCUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 10);
            
            var matchedOrders = await _matchingEngine.MatchOrderAsync(order, false);
            
            Assert.AreEqual(0, matchedOrders.Count);
        }
       
    }
}
