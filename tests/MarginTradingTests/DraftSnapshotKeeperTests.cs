// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests
{
    [TestFixture]
    public class DraftSnapshotKeeperTests
    {
        private Mock<ITradingEngineSnapshotsRepository> _repositoryMock;

        private static readonly object[] UpdateCases =
        {
            new object[] { null, null, null },
            new object[]
            {
                null, 
                ImmutableArray.Create<Order>(GetDumbOrder()),
                ImmutableArray.Create<MarginTradingAccount>(new MarginTradingAccount())
            },
            new object[]
            {
                ImmutableArray.Create<Position>(new Position()), 
                null,
                ImmutableArray.Create<MarginTradingAccount>(new MarginTradingAccount())
            },
            new object[]
            {
                ImmutableArray.Create<Position>(new Position()), 
                ImmutableArray.Create<Order>(GetDumbOrder()), 
                null
            },
            new object[]
            {
                ImmutableArray.Create<Position>(),
                ImmutableArray.Create<Order>(),
                ImmutableArray.Create<MarginTradingAccount>()
            },
            new object[]
            {
                ImmutableArray.Create<Position>(),
                ImmutableArray.Create<Order>(GetDumbOrder()),
                ImmutableArray.Create<MarginTradingAccount>(new MarginTradingAccount())
            },
            new object[]
            {
                ImmutableArray.Create<Position>(new Position()),
                ImmutableArray.Create<Order>(),
                ImmutableArray.Create<MarginTradingAccount>(new MarginTradingAccount())
            },
            new object[]
            {
                ImmutableArray.Create<Position>(new Position()),
                ImmutableArray.Create<Order>(GetDumbOrder()),
                ImmutableArray.Create<MarginTradingAccount>()
            },
        };
        
        [SetUp]
        public void SetUp()
        {
            _repositoryMock = new Mock<ITradingEngineSnapshotsRepository>();
            _repositoryMock
                .Setup(r => r.DraftExistsAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(true);
            _repositoryMock
                .Setup(r => r.GetLastDraftAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(GetDumbDraft);
        }
        
        [Test]
        public void AccessTradingDay_BeforeInitialization_ThrowsException()
        {
            var keeper = new DraftSnapshotKeeper(_repositoryMock.Object);

            Assert.Throws<InvalidOperationException>(() =>
            {
                var _ = keeper.TradingDay;
            });
        }

        [Test]
        public void Exists_BeforeInitialization_ThrowsException()
        {
            var keeper = new DraftSnapshotKeeper(_repositoryMock.Object);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await keeper.ExistsAsync());
        }

        [Test]
        public async Task Exists_ChecksIfDraftExists()
        {
            var keeper = GetSut();
            
            var _ = await keeper.ExistsAsync();
            
            _repositoryMock.Verify(r => r.DraftExistsAsync(It.IsAny<DateTime>()), Times.Once);
        }

        [Test]
        public void GetAccounts_BeforeInitialization_ThrowsException()
        {
            var keeper = new DraftSnapshotKeeper(_repositoryMock.Object);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await keeper.GetAccountsAsync());
        }

        [Test]
        public void GetAccounts_NoDraft_ThrowsException()
        {
            _repositoryMock
                .Setup(r => r.GetLastDraftAsync(It.IsAny<DateTime>()))
                .ReturnsAsync((TradingEngineSnapshot)null);

            var keeper = GetSut();

            Assert.ThrowsAsync<TradingSnapshotDraftNotFoundException>(async () => await keeper.GetAccountsAsync());
        }

        [Test]
        public async Task GetAccounts_ReturnsCached_WhenAccessed_MoreThanOnce()
        {
            var keeper = GetSut();
            
            var accounts = await keeper.GetAccountsAsync();
            
            Assert.NotNull(accounts);
            Assert.That(accounts.Exists(a => a.Id == "1"));

            _repositoryMock
                .Setup(r => r.GetLastDraftAsync(It.IsAny<DateTime>()))
                .ReturnsAsync((TradingEngineSnapshot)null);

            var accountsCached = await keeper.GetAccountsAsync();
            Assert.That(accounts.SequenceEqual(accountsCached));
        }

        [TestCaseSource(nameof(UpdateCases))]
        public void Update_InvalidArguments_ThrowsException(
            ImmutableArray<Position> positions, 
            ImmutableArray<Order> orders, 
            ImmutableArray<MarginTradingAccount> accounts)
        {
            var keeper = GetSut();

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await keeper.UpdateAsync(positions, orders, accounts));
        }

        private IDraftSnapshotKeeper GetSut() => new DraftSnapshotKeeper(_repositoryMock.Object).Init(DateTime.UtcNow);

        private static TradingEngineSnapshot GetDumbDraft() =>
            new TradingEngineSnapshot(
                DateTime.UtcNow,
                string.Empty,
                DateTime.UtcNow,
                string.Empty,
                string.Empty,
                new List<MarginTradingAccount>{new MarginTradingAccount{Id = "1"}}.ToJson(),
                string.Empty,
                string.Empty,
                SnapshotStatus.Draft);

        private static Order GetDumbOrder() =>
            new Order(
                "1", 
                default, 
                string.Empty, 
                default, 
                default, 
                default, 
                null, 
                string.Empty, 
                string.Empty, 
                string.Empty, 
                default, 
                string.Empty, 
                default, 
                string.Empty, 
                string.Empty, 
                default, 
                default, 
                string.Empty, 
                string.Empty, 
                default, 
                default, 
                default, 
                string.Empty, 
                default, 
                default, 
                string.Empty);
    }
}