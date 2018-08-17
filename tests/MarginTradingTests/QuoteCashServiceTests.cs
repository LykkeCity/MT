using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Services.Events;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    //[Ignore("Change logic")]
    public class QuoteCashServiceTests : BaseTests
    {
        private IQuoteCacheService _quoteCacheService;
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private ICfdCalculatorService _cfdCalculatorService;
        private IFxRateCacheService _fxRateCacheService;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _quoteCacheService = Container.Resolve<IQuoteCacheService>();
            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
            _cfdCalculatorService = Container.Resolve<ICfdCalculatorService>();
            _fxRateCacheService = Container.Resolve<IFxRateCacheService>();
        }

        [Test]
        public void Is_Quote_Returned()
        {
            const string instrument = "EURUSD";

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = instrument, Ask = 1.05M, Bid = 1.04M }));

            var quote = _quoteCacheService.GetQuote(instrument);

            Assert.IsNotNull(quote);
            Assert.AreEqual(1.04, quote.Bid);
            Assert.AreEqual(1.05, quote.Ask);
        }

        [Test]
        public void Is_GetQuoteRateForBaseAsset_Correct()
        {
            const string instrument = "BTCUSD";

            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = instrument, Ask = 905.35M, Bid = 905.1M }));

            var quote = _quoteCacheService.GetQuote(instrument);

            Assert.IsNotNull(quote);
            Assert.AreEqual(905.1, quote.Bid);
            Assert.AreEqual(905.35, quote.Ask);

            var quoteRate = _cfdCalculatorService.GetQuoteRateForBaseAsset(Accounts[0].BaseAssetId, instrument, "LYKKETEST");

            Assert.AreEqual(905.35, quoteRate);
            
            quoteRate = _cfdCalculatorService.GetQuoteRateForBaseAsset(Accounts[0].BaseAssetId, instrument, "LYKKETEST", false);
            
            Assert.AreEqual(905.1, quoteRate);
        }

        [Test]
        public void Is_GetQuoteRateForQuoteAsset_Correct()
        {
            const string instrument = "USDCHF";
            
            _fxRateCacheService.SetQuote(new InstrumentBidAskPair { Instrument = "USDCHF", Ask = 0.9982M, Bid = 0.9980M });

            var quote = _fxRateCacheService.GetQuote(instrument);

            Assert.IsNotNull(quote);
            Assert.AreEqual(0.9980, quote.Bid);
            Assert.AreEqual(0.9982, quote.Ask);

            var quoteRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(Accounts[0].BaseAssetId, instrument, "LYKKETEST");

            Assert.AreEqual(1.002004008016032064128256513, quoteRate);
            
            quoteRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(Accounts[0].BaseAssetId, instrument, "LYKKETEST", false);

            Assert.AreEqual(1.0018032458425165297535564015, quoteRate);
        }
    }
}
