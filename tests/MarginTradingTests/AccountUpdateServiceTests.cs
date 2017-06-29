﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Services;
using MarginTrading.Services.Events;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class AccountUpdateServiceTests : BaseTests
    {
        private IAccountsCacheService _accountsCacheService;
        private OrdersCache _ordersCache;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _accountsCacheService = Container.Resolve<IAccountsCacheService>();
            _ordersCache = Container.Resolve<OrdersCache>();
            var bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();

            bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "EURUSD", Bid = 1.02, Ask = 1.04 }));
            bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCUSD", Bid = 905.1, Ask = 905.35 }));
        }

        [Test]
        public void Check_Account_Calculations_Correct()
        {
            var order1 = new Order
            {
                Id = Guid.NewGuid().ToString("N"),
                Instrument = "EURUSD",
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Volume = 1000,
                MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 1000 } }, //need for GetMatchedVolume()
                OpenPrice = 1.02
            };

            _ordersCache.ActiveOrders.Add(order1);
            order1.UpdateClosePrice(1.04);

            order1.GetFpl();
            var account = _accountsCacheService.Get(order1.ClientId, order1.AccountId);

            Assert.IsNotNull(account);
            Assert.AreEqual(1000, account.Balance);
            Assert.AreEqual(20, Math.Round(account.GetPnl(), 5));
            Assert.AreEqual(1020, account.GetTotalCapital());
            Assert.AreEqual(6.93333, account.GetUsedMargin());
            Assert.AreEqual(1009.6, account.GetMarginAvailable());
            Assert.AreEqual(0.0067973823529411765, account.GetMarginUsageLevel());

            var order2 = new Order
            {
                Id = Guid.NewGuid().ToString("N"),
                Instrument = "EURUSD",
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Volume = -30000,
                MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 30000 } }, //need for GetMatchedVolume()
                OpenPrice = 1.02
            };

            _ordersCache.ActiveOrders.Add(order2);
            order2.UpdateClosePrice(1.04);
            order2.GetFpl();

            Assert.IsNotNull(account);
            Assert.AreEqual(1000, account.Balance);
            Assert.AreEqual(-580, Math.Round(account.GetPnl(), 5));
            Assert.AreEqual(420, Math.Round(account.GetTotalCapital(), 5));
            Assert.AreEqual(214.93333, account.GetUsedMargin());
            Assert.AreEqual(97.6, Math.Round(account.GetMarginAvailable(), 5));
            Assert.AreEqual(0.51175, Math.Round(account.GetMarginUsageLevel(), 5));
        }

        [Test]
        public void Check_Account_StopOut_Level()
        {
            var order = new Order
            {
                Id = Guid.NewGuid().ToString("N"),
                Instrument = "EURUSD",
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                Volume = 130000,
                MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 130000} }, //need for GetMatchedVolume()
                OpenPrice = 1.02
            };

            _ordersCache.ActiveOrders.Add(order);
            order.UpdateClosePrice(1.02);
            order.GetFpl();
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.GetMarginUsageLevel() >= 0.8);
        }
    }
}
