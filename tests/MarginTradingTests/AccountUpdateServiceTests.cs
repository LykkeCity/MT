using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Events;
using MoreLinq;
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
            foreach (var o in _ordersCache.ActiveOrders.GetAllOrders().ToList())
                _ordersCache.ActiveOrders.Remove(o);
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
                LegalEntity = "LYKKEVU",
                Volume = 1000,
                MatchedOrders =
                    new MatchedOrderCollection(new List<MatchedOrder>
                    {
                        new MatchedOrder {MatchedDate = DateTime.UtcNow, Volume = 1000}
                    }), //need for GetMatchedVolume()
                OpenPrice = 1.02M,
                Status = OrderStatus.Active,
            };

            _ordersCache.ActiveOrders.Add(order1);
            order1.UpdateClosePrice(1.04M);

            order1.GetFpl();
            var account = _accountsCacheService.Get(order1.ClientId, order1.AccountId);

            Assert.IsNotNull(account);
            Assert.AreEqual(1000, account.Balance);
            Assert.AreEqual(20, Math.Round(account.GetPnl(), 5));
            Assert.AreEqual(1020, account.GetTotalCapital());
            Assert.AreEqual(6.93333333m, account.GetUsedMargin());
            Assert.AreEqual(1009.60, account.GetMarginAvailable());
            Assert.AreEqual(147.11538468611316571447748352m, account.GetMarginUsageLevel());

            var order2 = new Order
            {
                Id = Guid.NewGuid().ToString("N"),
                Instrument = "EURUSD",
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                LegalEntity = "LYKKEVU",
                Volume = -30000,
                MatchedOrders = new MatchedOrderCollection(new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 30000 } }), //need for GetMatchedVolume()
                OpenPrice = 1.02M,
                Status = OrderStatus.Active,
            };

            _ordersCache.ActiveOrders.Add(order2);
            order2.UpdateClosePrice(1.04M);
            order2.GetFpl();

            Assert.IsNotNull(account);
            Assert.AreEqual(1000, account.Balance);
            Assert.AreEqual(-580, Math.Round(account.GetPnl(), 5));
            Assert.AreEqual(420, Math.Round(account.GetTotalCapital(), 5));
            Assert.AreEqual(214.93333333m, account.GetUsedMargin());
            Assert.AreEqual(97.60, Math.Round(account.GetMarginAvailable(), 5));
            Assert.AreEqual(1.95409m, Math.Round(account.GetMarginUsageLevel(), 5));
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
                LegalEntity = "LYKKEVU",
                Volume = 130000,
                MatchedOrders = new MatchedOrderCollection( new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 130000} }), //need for GetMatchedVolume()
                OpenPrice = 1.02M,
                Status = OrderStatus.Active,
            };

            _ordersCache.ActiveOrders.Add(order);
            order.UpdateClosePrice(1.02M);
            order.GetFpl();
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);

            Assert.IsNotNull(account);
            Assert.IsTrue(account.GetMarginUsageLevel() <= 1.25M);
        }
        
        [Test]
        public void Check_IsEnoughBalance()
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
                LegalEntity = "LYKKEVU",
                Volume = 96000,
                OpenPrice = 1.02M,
                Status = OrderStatus.Active,
            };
            
            var result1 = _accountUpdateService.IsEnoughBalance(order1);//account have 1000
            Assert.IsTrue(result1);
            
            var order2 = new Order
            {
                Id = Guid.NewGuid().ToString("N"),
                Instrument = "EURUSD",
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                AccountAssetId = Accounts[0].BaseAssetId,
                AssetAccuracy = 5,
                LegalEntity = "LYKKEVU",
                Volume = 97000,
                OpenPrice = 1.02M
            };
            
            var result2 = _accountUpdateService.IsEnoughBalance(order2);//account have 1000
            Assert.IsFalse(result2);
        }
    }
}
