// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Common.Services;
using MarginTradingTests.Helpers;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class FinalSnapshotCalculatorTests
    {
        private Mock<ICfdCalculatorService> _cfdCalculatorMock;
        private Mock<ILog> _logMock;
        private Mock<IDateService> _dateServiceMock;
        private Mock<IDraftSnapshotKeeper> _draftSnapshotKeeper;
        private Mock<IAccountsCacheService> _accountCacheServiceMock;

        [SetUp]
        public void SetUp()
        {
            _cfdCalculatorMock = new Mock<ICfdCalculatorService>();
            _logMock = new Mock<ILog>();
            _dateServiceMock = new Mock<IDateService>();
            _draftSnapshotKeeper = new Mock<IDraftSnapshotKeeper>();
            _accountCacheServiceMock = new Mock<IAccountsCacheService>();

            _draftSnapshotKeeper
                .Setup(k => k.GetAccountsAsync())
                .ReturnsAsync(new List<MarginTradingAccount> { GetDumbMarginTradingAccount() });

            _draftSnapshotKeeper
                .Setup(k => k.GetPositions())
                .Returns(ImmutableArray.Create(DumbDataGenerator.GeneratePosition()));

            _draftSnapshotKeeper
                .Setup(k => k.GetAllOrders())
                .Returns(ImmutableArray.Create(DumbDataGenerator.GenerateOrder()));

            _draftSnapshotKeeper
                .Setup(k => k.FxPrices)
                .Returns(new List<BestPriceContract>());

            _draftSnapshotKeeper
                .Setup(k => k.CfdQuotes)
                .Returns(new List<BestPriceContract>());

            _draftSnapshotKeeper
                .Setup(k => k.Timestamp)
                .Returns(DateTime.UtcNow);

            _accountCacheServiceMock
                .Setup(c => c.GetAllInLiquidation())
                .Returns(AsyncEnumerable.Empty<MarginTradingAccount>());
        }

        [Test]
        public async Task Run_Returns_Final_Snapshot_With_Same_Timestamp_As_Draft()
        {
            var sut = GetSut();

            var final = await sut.RunAsync(
                new[] { GetDumbFxRate() },
                new[] { GetDumbCfdQuote() },
                string.Empty);

            Assert.AreEqual(_draftSnapshotKeeper.Object.Timestamp, final.Timestamp);
        }

        private IFinalSnapshotCalculator GetSut() => new FinalSnapshotCalculator(
            _cfdCalculatorMock.Object,
            _logMock.Object,
            _dateServiceMock.Object,
            _draftSnapshotKeeper.Object,
            _accountCacheServiceMock.Object);

        private static ClosingFxRate GetDumbFxRate() => 
            new ClosingFxRate { AssetId = "dumbAssetId", ClosePrice = 1 };

        private static ClosingAssetPrice GetDumbCfdQuote() =>
            new ClosingAssetPrice { AssetId = "dumbAssetId", ClosePrice = 1 };

        private static MarginTradingAccount GetDumbMarginTradingAccount()
        {
            var result = new MarginTradingAccount();

            result.AccountFpl.ActualHash = 1;
            result.AccountFpl.CalculatedHash = 1;

            return result;
        }
    }
}