// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Autofac;
using Autofac.Extras.Moq;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Services;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class PositionsProviderTests
    {
        private static readonly object[] EmptyAccountIds = new object[]
        {
            null,
            new string[] { },
            new string[] { "" },
            new string[] { " " },
            new string[] { null }
        };
        
        
        [TestCaseSource(nameof(EmptyAccountIds))]
        public void GetPositionsByAccount_InvalidArguments_ThrowsException(string[] accountIds)
        {
            using (var mock = AutoMock.GetLoose())
            {
                var sut = mock.Create<PositionsProvider>();

                Assert.Throws<ArgumentNullException>(() => sut.GetPositionsByAccountIds(accountIds));
            }
        }

        [Test]
        public void GetPositionsByAccount_DraftSnapshotKeeperNotInitialized_OrdersCacheIsUsed()
        {
            using (var mock = AutoMock.GetLoose())
            {
                var ordersCache = mock.Mock<OrdersCache>();
                ordersCache
                    .SetupGet(c => c.Positions)
                    .Returns(new PositionsCache(new List<Position>()));

                var sut = mock.Create<PositionsProvider>();
                
                var _ = sut.GetPositionsByAccountIds("accountId");

                ordersCache.VerifyGet(c => c.Positions, Times.Once);
            }
        }

        [Test]
        public void GetPositionsByAccount_DraftSnapshotKeeperInitialized_And_Used_As_PositionsSource()
        {
            var draftSnapshotKeeperMock = new Mock<IDraftSnapshotKeeper>();
            draftSnapshotKeeperMock
                .SetupGet(d => d.TradingDay)
                .Returns(DateTime.UtcNow.Date);
            draftSnapshotKeeperMock
                .Setup(d => d.GetPositions())
                .Returns(ImmutableArray.Create<Position>());

            using (var mock = AutoMock.GetLoose(BeforeBuild))
            {
                var sut = mock.Create<PositionsProvider>();

                var _ = sut.GetPositionsByAccountIds("accountId");

                draftSnapshotKeeperMock.Verify(d => d.GetPositions(), Times.Once);
            }
            
            void BeforeBuild(ContainerBuilder builder)
            {
                builder
                    .RegisterInstance(draftSnapshotKeeperMock.Object)
                    .As<IDraftSnapshotKeeper>()
                    .ExternallyOwned();
            }
        }
    }
}