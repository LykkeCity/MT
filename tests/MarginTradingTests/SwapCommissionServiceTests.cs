using System;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Services;
using MarginTrading.Services.Events;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class SwapCommissionServiceTests : BaseTests
    {
        private ISwapCommissionService _swapService;
        private IAccountAssetPairsRepository _accountAssetsPairsRepository;
        private string _accountId;
        private string _accountAssetId;
        private string _tradingConditionId;
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private AccountAssetsManager _accountAssetsManager;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();
            _accountId = Accounts[0].Id;
            _accountAssetId = Accounts[0].BaseAssetId;
            _tradingConditionId = Accounts[0].TradingConditionId;

            _accountAssetsManager = Container.Resolve<AccountAssetsManager>();
            _swapService = Container.Resolve<ISwapCommissionService>();
            _accountAssetsPairsRepository = Container.Resolve<IAccountAssetPairsRepository>();
            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
        }

        [Test]
        public void Is_GetSwapCount_Correct()
        {
            var count = _swapService.GetSwapCount(new DateTime(2017, 01, 01, 20, 50, 0), new DateTime(2017, 01, 01, 20, 55, 0));

            Assert.AreEqual(0, count);

            count = _swapService.GetSwapCount(new DateTime(2017, 01, 01, 20, 50, 0), new DateTime(2017, 01, 01, 21, 05, 0));

            Assert.AreEqual(1, count);

            count = _swapService.GetSwapCount(new DateTime(2017, 01, 01, 20, 50, 0), new DateTime(2017, 01, 02, 21, 05, 0));

            Assert.AreEqual(2, count);

            count = _swapService.GetSwapCount(new DateTime(2017, 01, 01, 21, 50, 0), new DateTime(2017, 01, 02, 20, 55, 0));

            Assert.AreEqual(0, count);

            count = _swapService.GetSwapCount(new DateTime(2017, 01, 01, 21, 50, 0), new DateTime(2017, 01, 02, 22, 00, 0));

            Assert.AreEqual(1, count);

            count = _swapService.GetSwapCount(new DateTime(2017, 01, 01, 21, 0, 0), new DateTime(2017, 01, 01, 21, 05, 0));

            Assert.AreEqual(1, count);

            count = _swapService.GetSwapCount(new DateTime(2017, 01, 01, 21, 0, 0), new DateTime(2017, 01, 02, 21, 0, 0));

            Assert.AreEqual(2, count);
        }

        [Test]
        public async Task Is_Swaps_Correct()
        {
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "EURUSD", Bid = 1.02M, Ask = 1.04M }));

            _accountAssetsPairsRepository.AddOrReplaceAsync(new AccountAssetPair
            {
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                BaseAssetId = "USD",
                Instrument = "EURUSD",
                LeverageInit = 100,
                LeverageMaintenance = 150,
                SwapLong = 100,
                SwapShort = 100
            }).Wait();

            await _accountAssetsManager.UpdateAccountAssetsCache();

            var swapsForDay = _swapService.GetSwaps(_tradingConditionId, _accountId, _accountAssetId, "EURUSD", OrderDirection.Buy, new DateTime(2017, 01, 01, 20, 50, 0),
                new DateTime(2017, 01, 02, 20, 50, 0), 20);

            var swapsFor2Days = _swapService.GetSwaps(_tradingConditionId, _accountId, _accountAssetId, "EURUSD", OrderDirection.Buy, new DateTime(2017, 01, 01, 20, 50, 0),
                new DateTime(2017, 01, 03, 20, 50, 0), 20);

            Assert.AreEqual(0, swapsForDay);
            Assert.AreEqual(0.00001, swapsFor2Days);
        }

        [Test]
        public async Task Is_Swaps_Pct_Correct()
        {
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "EURUSD", Bid = 1.02M, Ask = 1.04M}));

            await _accountAssetsPairsRepository.AddOrReplaceAsync(new AccountAssetPair
            {
                TradingConditionId = MarginTradingTestsUtils.TradingConditionId,
                BaseAssetId = "USD",
                Instrument = "EURUSD",
                LeverageInit = 100,
                LeverageMaintenance = 150,
                SwapLongPct = 10,
                SwapShortPct = 10
            });

            await _accountAssetsManager.UpdateAccountAssetsCache();

            var swapsForHour = _swapService.GetSwaps(_tradingConditionId, _accountId, _accountAssetId, "EURUSD", OrderDirection.Buy, new DateTime(2017, 01, 01, 20, 50, 0),
                new DateTime(2017, 01, 01, 21, 50, 0), 200);

            var swapsForDay = _swapService.GetSwaps(_tradingConditionId, _accountId, _accountAssetId, "EURUSD", OrderDirection.Buy, new DateTime(2017, 01, 01, 20, 50, 0),
                new DateTime(2017, 01, 02, 20, 50, 0), 200);

            var swapsFor2Days = _swapService.GetSwaps(_tradingConditionId, _accountId, _accountAssetId, "EURUSD", OrderDirection.Buy, new DateTime(2017, 01, 01, 20, 50, 0),
                new DateTime(2017, 01, 03, 20, 50, 0), 200);

            Assert.AreEqual(0.23744, swapsForHour);
            Assert.AreEqual(5.69863, swapsForDay);
            Assert.AreEqual(11.39726, swapsFor2Days);
        }
    }
}
