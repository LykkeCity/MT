using System;
using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Services.Events;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class MarketMakerTests : BaseTests
    {
        private IEventConsumer<BestPriceChangeEventArgs> _quoteCashService;
        private IMarketMakerMatchingEngine _matchingEngine;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            RegisterDependencies();
            _quoteCashService = Container.Resolve<IQuoteCacheService>() as IEventConsumer<BestPriceChangeEventArgs>;
            _matchingEngine = Container.Resolve<IMarketMakerMatchingEngine>();
        }

        [SetUp]
        public void SetUp()
        {
            _quoteCashService.ConsumeEvent(this,
                new BestPriceChangeEventArgs(new InstrumentBidAskPair
                {
                    Instrument = "EURUSD",
                    Date = DateTime.Now,
                    Bid = 1.04M,
                    Ask = 1.1M
                }));
        }

        [TearDown]
        public void TeadDown()
        {
            _matchingEngine.SetOrders("marketMaker1", null, null, true);
        }
    }
}
