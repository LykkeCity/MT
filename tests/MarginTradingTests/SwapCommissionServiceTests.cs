using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Core.MatchedOrders;
using MarginTrading.Services;
using MarginTrading.Services.Events;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class SwapCommissionServiceTests : BaseTests
    {
        private ICommissionService _swapService;
        private IAccountAssetPairsRepository _accountAssetsRepository;
        private IEventChannel<BestPriceChangeEventArgs> _bestPriceConsumer;
        private AccountAssetsManager _accountAssetsManager;

        [OneTimeSetUp]
        public void SetUp()
        {
            RegisterDependencies();

            _accountAssetsManager = Container.Resolve<AccountAssetsManager>();
            _swapService = Container.Resolve<ICommissionService>();
            _accountAssetsRepository = Container.Resolve<IAccountAssetPairsRepository>();
            _bestPriceConsumer = Container.Resolve<IEventChannel<BestPriceChangeEventArgs>>();
        }

        [Test]
        public async Task Is_Swaps_Correct()
        {
            _bestPriceConsumer.SendEvent(this, new BestPriceChangeEventArgs(new InstrumentBidAskPair { Instrument = "EURUSD", Bid = 1.02M, Ask = 1.04M }));

            _accountAssetsRepository.AddOrReplaceAsync(new AccountAssetPair
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

            var dayOrder = new Order
            {
                AccountAssetId = "USD",
                Instrument = "EURUSD",
                Volume = 20,
                OpenDate = new DateTime(2017, 01, 01, 20, 50, 0),
                CloseDate = new DateTime(2017, 01, 02, 20, 50, 0),
                MatchedOrders = new MatchedOrderCollection(new List<MatchedOrder> { new MatchedOrder() { Volume = 20} }),
                SwapCommission = 100
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
                SwapCommission = 100
            };

            var swapsFor2Days = _swapService.GetSwaps(twoDayOrder);

            Assert.AreEqual(5.69863, swapsForDay);
            Assert.AreEqual(11.39726, swapsFor2Days);
        }
    }
}
