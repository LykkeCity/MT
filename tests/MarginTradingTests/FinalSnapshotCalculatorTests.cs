// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Common.Services;
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

        [SetUp]
        public void SetUp()
        {
            _cfdCalculatorMock = new Mock<ICfdCalculatorService>();
            _logMock = new Mock<ILog>();
            _dateServiceMock = new Mock<IDateService>();
            _draftSnapshotKeeper = new Mock<IDraftSnapshotKeeper>();

            _draftSnapshotKeeper
                .Setup(k => k.GetAccountsAsync())
                .ReturnsAsync(new List<MarginTradingAccount> { GetDumbMarginTradingAccount() });

            _draftSnapshotKeeper
                .Setup(k => k.GetPositions())
                .Returns(ImmutableArray.Create(GetDumbPosition()));

            _draftSnapshotKeeper
                .Setup(k => k.GetAllOrders())
                .Returns(ImmutableArray.Create(DraftSnapshotKeeperTests.GetDumbOrder()));

            _draftSnapshotKeeper
                .Setup(k => k.FxPrices)
                .Returns(new List<BestPriceContract>());

            _draftSnapshotKeeper
                .Setup(k => k.CfdQuotes)
                .Returns(new List<BestPriceContract>());

            _draftSnapshotKeeper
                .Setup(k => k.Timestamp)
                .Returns(DateTime.UtcNow);
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
            _draftSnapshotKeeper.Object);

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

        private static Position GetDumbPosition()
        {
            var result = new Position("1",
                1,
                string.Empty,
                default,
                string.Empty,
                string.Empty,
                string.Empty,
                null,
                string.Empty,
                default,
                string.Empty,
                default,
                default,
                default,
                default,
                string.Empty,
                default,
                new List<RelatedOrderInfo>(),
                string.Empty,
                default,
                string.Empty,
                string.Empty,
                default,
                string.Empty,
                default);

            result.FplData.CalculatedHash = 1;

            return result;
        }
    }
}