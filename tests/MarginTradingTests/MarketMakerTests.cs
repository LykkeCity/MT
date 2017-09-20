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
    }
}
