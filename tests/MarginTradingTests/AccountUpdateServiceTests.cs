using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MarginTradingTests.Helpers;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class AccountUpdateServiceTests : BaseTests
    {
        private IAccountsCacheService _accountsCacheService;
        private IAccountUpdateService _accountUpdateService;
        private OrdersCache _ordersCache;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RegisterDependencies();
            _accountsCacheService = Container.Resolve<IAccountsCacheService>();
            _accountUpdateService = Container.Resolve<IAccountUpdateService>();
            _ordersCache = Container.Resolve<OrdersCache>();
            var bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();

            bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "EURUSD", Bid = 1.02M, Ask = 1.04M }));
            bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCUSD", Bid = 905.1M, Ask = 905.35M }));
        }

        [SetUp]
        public void SetUp()
        {
            foreach (var o in _ordersCache.Positions.GetAllPositions().ToList())
                _ordersCache.Positions.Remove(o);
        }

        [Test]
        public void Check_Account_Calculations_Correct()
        {
            var position1 = TestObjectsFactory.CreateOpenedPosition("EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 1000, 1.02M);

            _ordersCache.Positions.Add(position1);
            position1.UpdateClosePrice(1.04M);

            position1.GetFpl();
            var account = _accountsCacheService.Get(position1.AccountId);

            Assert.IsNotNull(account);
            Assert.AreEqual(1000, account.Balance);
            Assert.AreEqual(20, Math.Round(account.GetPnl(), 5));
            Assert.AreEqual(1020, account.GetTotalCapital());
            Assert.AreEqual(6.8M, account.GetUsedMargin());
            Assert.AreEqual(1009.8M, account.GetMarginAvailable());
            Assert.AreEqual(150M, account.GetMarginUsageLevel());

            var position2 = TestObjectsFactory.CreateOpenedPosition("EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, -30000, 1.02M);
            
            _ordersCache.Positions.Add(position2);
            position2.UpdateClosePrice(1.04M);
            position2.GetFpl();

            Assert.IsNotNull(account);
            Assert.AreEqual(1000, account.Balance);
            Assert.AreEqual(-580, Math.Round(account.GetPnl(), 5));
            Assert.AreEqual(420, Math.Round(account.GetTotalCapital(), 5));
            Assert.AreEqual(214.8m, account.GetUsedMargin());
            Assert.AreEqual(97.8m, Math.Round(account.GetMarginAvailable(), 5));
            Assert.AreEqual(1.95531m, Math.Round(account.GetMarginUsageLevel(), 5));
        }

        [Test]
        public void Check_Account_StopOut_Level()
        {
            var position1 = TestObjectsFactory.CreateOpenedPosition("EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 130000, 1.02M);
            
            _ordersCache.Positions.Add(position1);
            position1.UpdateClosePrice(1.02M);
            position1.GetFpl();
            var account = _accountsCacheService.Get(position1.AccountId);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.GetMarginUsageLevel() <= 1.25M);
        }
        
        [Test]
        public void Check_IsEnoughBalance()
        {
            var order1 = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 96000);
            
            var result1 = _accountUpdateService.IsEnoughBalance(order1);//account have 1000
            Assert.IsTrue(result1);
            
            var order2 = TestObjectsFactory.CreateNewOrder(OrderType.Market, "EURUSD", Accounts[0],
                MarginTradingTestsUtils.TradingConditionId, 97000);
            
            var result2 = _accountUpdateService.IsEnoughBalance(order2);//account have 1000
            Assert.IsFalse(result2);
        }
    }
}
