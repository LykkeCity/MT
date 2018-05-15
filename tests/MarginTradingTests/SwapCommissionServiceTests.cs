using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.TradingConditions;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class SwapCommissionServiceTests : BaseTests
    {
        private ICommissionService _swapService;
        private ITradingInstrumentsApi _tradingInstruments;
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private TradingInstrumentsManager _accountAssetsManager;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();

            _accountAssetsManager = Container.Resolve<TradingInstrumentsManager>();
            _swapService = Container.Resolve<ICommissionService>();
            _tradingInstruments = Container.Resolve<ITradingInstrumentsApi>();
            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
        }

        [Test]
        public async Task Is_Swaps_Correct()
        {
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "EURUSD", Bid = 1.02M, Ask = 1.04M }));

            var instrumentContract = new TradingInstrumentContract
            {
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                Instrument = "EURUSD",
                LeverageInit = 100,
                LeverageMaintenance = 150,
                SwapLong = 100,
                SwapShort = 100
            };

            Mock.Get(_tradingInstruments).Setup(s => s.List(It.IsAny<string>()))
                .ReturnsAsync(new List<TradingInstrumentContract> {instrumentContract});

            await _accountAssetsManager.UpdateTradingInstrumentsCache();

            var dayOrder = new Order
            {
                AccountAssetId = "USD",
                Instrument = "EURUSD",
                Volume = 20,
                OpenDate = new DateTime(2017, 01, 01, 20, 50, 0),
                CloseDate = new DateTime(2017, 01, 02, 20, 50, 0),
                MatchedOrders = new MatchedOrderCollection(new List<MatchedOrder> { new MatchedOrder() { Volume = 20} }),
                SwapCommission = 100,
                LegalEntity = "LYKKETEST",
            };

            var swapsForDay = _swapService.GetSwaps(dayOrder);

            var twoDayOrder = new Order
            {
                AccountAssetId = "USD",
                Instrument = "EURUSD",
                Volume = 20,
                OpenDate = new DateTime(2017, 01, 01, 20, 50, 0),
                CloseDate = new DateTime(2017, 01, 03, 20, 50, 0),
                MatchedOrders = new MatchedOrderCollection(new List<MatchedOrder>() { new MatchedOrder() { Volume = 20 } }),
                SwapCommission = 100,
                LegalEntity = "LYKKETEST",
            };

            var swapsFor2Days = _swapService.GetSwaps(twoDayOrder);

            Assert.AreEqual(5.69863014m, swapsForDay);
            Assert.AreEqual(11.39726027m, swapsFor2Days);
        }
    }
}
