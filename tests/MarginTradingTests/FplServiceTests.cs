using System;
using System.Collections.Generic;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Services;
using MarginTrading.Services.Events;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class FplServiceTests : BaseTests
    {
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private IAccountsCacheService _accountsCacheService;
        private OrdersCache _ordersCache;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
            _accountsCacheService = Container.Resolve<IAccountsCacheService>();
            _ordersCache = Container.Resolve<OrdersCache>();
        }

        [Test]
        public void Is_Fpl_Buy_Correct()
        {
            const string instrument = "BTCUSD";
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument  = instrument, Ask = 800, Bid = 790 }));

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                AccountAssetId = Accounts[0].BaseAssetId,
                TradingConditionId = Accounts[0].TradingConditionId,
                Instrument = instrument,
                Volume = 10,
                MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 10 } }, //need for GetMatchedVolume()
                OpenPrice = 790
            };

            order.UpdateClosePrice(800);

            Assert.AreEqual(100, order.GetFpl());
        }

        [Test]
        public void Is_Fpl_Sell_Correct()
        {
            const string instrument = "BTCUSD";
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = instrument, Ask = 800, Bid = 790 }));

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                AccountAssetId = Accounts[0].BaseAssetId,
                TradingConditionId = Accounts[0].TradingConditionId,
                Instrument = instrument,
                Volume = -10,
                MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 10 } }, //need for GetMatchedVolume()
                OpenPrice = 790
            };

            order.UpdateClosePrice(800);

            Assert.AreEqual(-100, order.GetFpl());
        }

        [Test]
        public void Is_Fpl_Correct_With_Commission()
        {
            const string instrument = "BTCUSD";
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = instrument, Ask = 800, Bid = 790 }));

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                AccountAssetId = Accounts[0].BaseAssetId,
                TradingConditionId = Accounts[0].TradingConditionId,
                Instrument = instrument,
                Volume = 10,
                MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 10 } }, //need for GetMatchedVolume()
                OpenPrice = 790,
                OpenCommission = 2,
                CommissionLot = 10
            };

            order.UpdateClosePrice(800);

            Assert.AreEqual(98, order.GetTotalFpl());
        }

        [Test]
        public void Is_Fpl_Buy_Cross_Correct()
        {
            const string instrument = "BTCCHF";

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Ask = 1.072030, Bid = 1.071940 }));

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                AccountAssetId = Accounts[0].BaseAssetId,
                TradingConditionId = Accounts[0].TradingConditionId,
                Instrument = instrument,
                Volume = 1000,
                MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 1000 } }, //need for GetMatchedVolume()
                OpenPrice = 935.461
            };

            order.UpdateClosePrice(935.61);

            Assert.AreEqual(138.989, Math.Round(order.GetFpl(), 3));
        }

        [Test]
        public void Is_Fpl_Sell_Cross_Correct()
        {
            const string instrument = "BTCCHF";

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "USDCHF", Ask = 1.072030, Bid = 1.071940 }));

            var order = new Order
            {
                Id = Guid.NewGuid().ToString(),
                AccountId = Accounts[0].Id,
                ClientId = Accounts[0].ClientId,
                AccountAssetId = Accounts[0].BaseAssetId,
                TradingConditionId = Accounts[0].TradingConditionId,
                Instrument = instrument,
                Volume = -1000,
                MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 1000 } }, //need for GetMatchedVolume()
                OpenPrice = 935.461,
            };

            order.UpdateClosePrice(935.61);
            var quoteRate = order.GetQuoteRate();

            Assert.AreEqual(0.93280971614600339, quoteRate);
            Assert.AreEqual(-138.989, Math.Round(order.GetFpl(), 3));
        }

        [Test]
        public void Check_Calculations_As_In_Excel_Document()
        {
            Accounts[0].Balance = 50000;
            _accountsCacheService.UpdateBalance(Accounts[0]);

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "EURUSD", Ask = 1.061, Bid = 1.06 }));
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "BTCEUR", Ask = 1092, Bid = 1091 }));

            var orders = new List<Order>
            {
                new Order
                {
                    CreateDate = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString("N"),
                    AccountId = Accounts[0].Id,
                    ClientId = Accounts[0].ClientId,
                    TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                    AccountAssetId = Accounts[0].BaseAssetId,
                    AssetAccuracy = 5,
                    Instrument = "EURUSD",
                    Volume = 100000,
                    MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 100000 } }, //need for GetMatchedVolume()
                    OpenPrice = 1.05,
                    FillType = OrderFillType.FillOrKill
                },
                new Order
                {
                    CreateDate = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString("N"),
                    AccountId = Accounts[0].Id,
                    ClientId = Accounts[0].ClientId,
                    TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                    AccountAssetId = Accounts[0].BaseAssetId,
                    AssetAccuracy = 5,
                    Instrument = "EURUSD",
                    Volume = -200000,
                    MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 200000 } }, //need for GetMatchedVolume()
                    OpenPrice = 1.04,
                    FillType = OrderFillType.FillOrKill
                },
                new Order
                {
                    CreateDate = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString("N"),
                    AccountId = Accounts[0].Id,
                    ClientId = Accounts[0].ClientId,
                    TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                    AccountAssetId = Accounts[0].BaseAssetId,
                    AssetAccuracy = 5,
                    Instrument = "EURUSD",
                    Volume = 50000,
                    MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 50000 } }, //need for GetMatchedVolume()
                    OpenPrice = 1.061,
                    FillType = OrderFillType.FillOrKill
                },
                new Order
                {
                    CreateDate = DateTime.UtcNow,
                    Id = Guid.NewGuid().ToString("N"),
                    AccountId = Accounts[0].Id,
                    ClientId = Accounts[0].ClientId,
                    TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                    AccountAssetId = Accounts[0].BaseAssetId,
                    AssetAccuracy = 3,
                    Instrument = "BTCEUR",
                    Volume = 100,
                    MatchedOrders = new List<MatchedOrder> { new MatchedOrder { MatchedDate = DateTime.UtcNow, Volume = 100 } }, //need for GetMatchedVolume()
                    OpenPrice = 1120,
                    FillType = OrderFillType.FillOrKill
                }
            };

            foreach (var order in orders)
            {
                _ordersCache.ActiveOrders.Add(order);
            }

            orders[0].UpdateClosePrice(1.06);
            orders[1].UpdateClosePrice(1.061);
            orders[2].UpdateClosePrice(1.06);
            orders[3].UpdateClosePrice(1091);

            var account = Accounts[0];

            Assert.AreEqual(50000, account.Balance);
            Assert.AreEqual(43676, Math.Round(account.GetTotalCapital(), 5));
            Assert.AreEqual(33491.6, Math.Round(account.GetFreeMargin(), 1));
            Assert.AreEqual(28399.4, Math.Round(account.GetMarginAvailable(), 1));
            Assert.AreEqual(-6324, Math.Round(account.GetPnl(), 5));
            Assert.AreEqual(10184.4, Math.Round(account.GetUsedMargin(), 1));
            Assert.AreEqual(15276.6, Math.Round(account.GetMarginInit(), 1));

        }
    }
}
