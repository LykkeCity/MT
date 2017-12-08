using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Core.MarketMakerFeed;
using MarginTrading.Services.Events;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class MarketMakerTests : BaseTests
    {
        private IEventConsumer<BestPriceChangeEventArgs> _quoteCashService;
        private IFeedConsumer _marketMaker;
        private IMatchingEngine _matchingEngine;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RegisterDependencies();
            _quoteCashService = Container.Resolve<IQuoteCacheService>() as IEventConsumer<BestPriceChangeEventArgs>;
            _marketMaker = Container.Resolve<IFeedConsumer>();
            _matchingEngine = Container.Resolve<IMatchingEngine>();
        }

        [SetUp]
        public void SetUp()
        {
            _quoteCashService.ConsumeEvent(this,
                new BestPriceChangeEventArgs(new InstrumentBidAskPair
                {
                    Instrument = "EURUSD",
                    Date = DateTime.Now,
                    Bid = 1.04,
                    Ask = 1.1
                }));
        }

        [TearDown]
        public void TeadDown()
        {
            _matchingEngine.SetOrders("marketMaker1", null, null, true);
        }

        [Test]
        public void Check_Limit_With_Gap_Buy_Creation()
        {
            var feedData = new AssetPairRate
            {
                AssetPairId = "EURUSD",
                DateTime = DateTime.Now,
                IsBuy = true,
                Price = 1.2,
                Volume = 10
            };

            _marketMaker.ConsumeFeed(feedData);

            var orderBook = _matchingEngine.GetOrderBook(new List<string>() {"marketMaker1"});

            Assert.AreEqual(0, orderBook.Count);

            feedData = new AssetPairRate
            {
                AssetPairId = "EURUSD",
                DateTime = DateTime.Now,
                IsBuy = false,
                Price = 1.3,
                Volume = -10
            };

            _marketMaker.ConsumeFeed(feedData);

            orderBook = _matchingEngine.GetOrderBook(new List<string>() { "marketMaker1" });

            Assert.AreEqual(1, orderBook.Count);
            Assert.AreEqual(1, orderBook["EURUSD"].Buy.Count);
            Assert.AreEqual(1, orderBook["EURUSD"].Sell.Count);
        }

        [Test]
        public void Check_Limit_With_Gap_Sell_Creation()
        {
            var feedData = new AssetPairRate
            {
                AssetPairId = "EURUSD",
                DateTime = DateTime.Now,
                IsBuy = false,
                Price = 1.02,
                Volume = -10
            };

            _marketMaker.ConsumeFeed(feedData);

            var orderBook = _matchingEngine.GetOrderBook(new List<string>() { "marketMaker1" });

            Assert.AreEqual(0, orderBook.Count);

            feedData = new AssetPairRate
            {
                AssetPairId = "EURUSD",
                DateTime = DateTime.Now,
                IsBuy = true,
                Price = 1.01,
                Volume = 10
            };

            _marketMaker.ConsumeFeed(feedData);

            orderBook = _matchingEngine.GetOrderBook(new List<string>() { "marketMaker1" });

            Assert.AreEqual(1, orderBook.Count);
            Assert.AreEqual(1, orderBook["EURUSD"].Buy.Count);
            Assert.AreEqual(1, orderBook["EURUSD"].Sell.Count);
        }
    }
}
