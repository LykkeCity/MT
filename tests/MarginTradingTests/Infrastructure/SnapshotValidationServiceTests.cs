// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Common;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Infrastructure;
using Moq;
using NUnit.Framework;

namespace MarginTradingTests.Infrastructure
{
    [TestFixture]
    public class SnapshotValidationServiceTests
    {
        private readonly Mock<ITradingEngineSnapshotsRepository> _tradingEngineSnapshotsRepositoryMock =
            new Mock<ITradingEngineSnapshotsRepository>();

        private readonly Mock<IOrdersHistoryRepository> _ordersHistoryRepositoryMock =
            new Mock<IOrdersHistoryRepository>();

        private readonly Mock<IPositionsHistoryRepository> _positionsHistoryRepositoryMock =
            new Mock<IPositionsHistoryRepository>();

        private readonly Mock<IOrderReader> _orderCacheMock =
            new Mock<IOrderReader>();

        private List<Order> _currentOrders;
        private List<Position> _currentPositions;
        private List<OrderHistory> _ordersHistory;
        private List<IPositionHistory> _positionsHistory;
        private TradingEngineSnapshot _tradingEngineSnapshot;

        private SnapshotValidationService _service;

        [SetUp]
        public void SetUp()
        {
            _currentOrders = new List<Order>();
            _currentPositions = new List<Position>();
            _ordersHistory = new List<OrderHistory>();
            _positionsHistory = new List<IPositionHistory>();
            _tradingEngineSnapshot = new TradingEngineSnapshot {Timestamp = DateTime.Now};

            _tradingEngineSnapshotsRepositoryMock.Setup(o => o.GetLastAsync())
                .ReturnsAsync(() => _tradingEngineSnapshot);

            _orderCacheMock.Setup(o => o.GetAllOrders())
                .Returns(() => _currentOrders.ToImmutableArray());

            _orderCacheMock.Setup(o => o.GetPositions())
                .Returns(() => _currentPositions.ToImmutableArray());

            _ordersHistoryRepositoryMock.Setup(o => o.GetLastSnapshot(It.IsAny<DateTime>()))
                .ReturnsAsync((DateTime date) => _ordersHistory);

            _positionsHistoryRepositoryMock.Setup(o => o.GetLastSnapshot(It.IsAny<DateTime>()))
                .ReturnsAsync((DateTime date) => _positionsHistory);

            _service = new SnapshotValidationService(
                _tradingEngineSnapshotsRepositoryMock.Object,
                _ordersHistoryRepositoryMock.Object,
                _positionsHistoryRepositoryMock.Object,
                _orderCacheMock.Object);
        }

        [Test]
        public async Task Restored_Orders_State_Equals_To_Current_State()
        {
            // arrange

            _tradingEngineSnapshot.Orders = new[]
            {
                CreateSnapshotOrder("1", 1, 1, OrderStatusContract.Active),
                CreateSnapshotOrder("2", 2, 2, OrderStatusContract.Active),
                CreateSnapshotOrder("3", 3, 3, OrderStatusContract.Active)
            }.ToJson();

            _currentOrders = new List<Order>
            {
                CreateOrder("1", 1, 1, OrderStatus.Active),
                CreateOrder("3", 3.1m, 3.1m, OrderStatus.Active),
                CreateOrder("4", 4, 4, OrderStatus.Active),
            };

            _ordersHistory = new List<OrderHistory>
            {
                CreateOrderHistory("1", 1, 1, OrderStatus.Active),
                CreateOrderHistory("2", 2, 2, OrderStatus.Executed),
                CreateOrderHistory("3", 3.1m, 3.1m, OrderStatus.Active),
                CreateOrderHistory("4", 4, 4, OrderStatus.Active)
            };

            // act

            var result = await _service.ValidateCurrentStateAsync();

            // assert

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task Current_State_Has_Extra_Orders()
        {
            // arrange

            _tradingEngineSnapshot.Orders = new[]
            {
                CreateSnapshotOrder("1", 1, 1, OrderStatusContract.Active),
                CreateSnapshotOrder("2", 2, 2, OrderStatusContract.Active),
                CreateSnapshotOrder("3", 3, 3, OrderStatusContract.Active)
            }.ToJson();

            _currentOrders = new List<Order>
            {
                CreateOrder("1", 1, 1, OrderStatus.Active),
                CreateOrder("3", 3.1m, 3.1m, OrderStatus.Active),
                CreateOrder("4", 4, 4, OrderStatus.Active)
            };

            _ordersHistory = new List<OrderHistory>
            {
                CreateOrderHistory("1", 1, 1, OrderStatus.Active),
                CreateOrderHistory("2", 2, 2, OrderStatus.Executed),
                CreateOrderHistory("3", 3.1m, 3.1m, OrderStatus.Active)
            };

            // act

            var result = await _service.ValidateCurrentStateAsync();

            // assert

            Assert.AreEqual(result.Orders.Extra.Count, 1);
        }

        [Test]
        public async Task Current_State_Has_Missed_Orders()
        {
            // arrange

            _tradingEngineSnapshot.Orders = new[]
            {
                CreateSnapshotOrder("1", 1, 1, OrderStatusContract.Active),
                CreateSnapshotOrder("2", 2, 2, OrderStatusContract.Active),
                CreateSnapshotOrder("3", 3, 3, OrderStatusContract.Active)
            }.ToJson();

            _currentOrders = new List<Order>
            {
                CreateOrder("1", 1, 1, OrderStatus.Active),
                CreateOrder("3", 3.1m, 3.1m, OrderStatus.Active)
            };

            _ordersHistory = new List<OrderHistory>
            {
                CreateOrderHistory("1", 1, 1, OrderStatus.Active),
                CreateOrderHistory("2", 2, 2, OrderStatus.Executed),
                CreateOrderHistory("3", 3.1m, 3.1m, OrderStatus.Active),
                CreateOrderHistory("4", 4, 4, OrderStatus.Active)
            };

            // act

            var result = await _service.ValidateCurrentStateAsync();

            // assert

            Assert.AreEqual(result.Orders.Missed.Count, 1);
        }

        [Test]
        public async Task Current_State_Has_Inconsistent_Orders()
        {
            // arrange

            _tradingEngineSnapshot.Orders = new[]
            {
                CreateSnapshotOrder("1", 1, 1, OrderStatusContract.Active),
                CreateSnapshotOrder("2", 2, 2, OrderStatusContract.Active),
                CreateSnapshotOrder("3", 3, 3, OrderStatusContract.Active)
            }.ToJson();

            _currentOrders = new List<Order>
            {
                CreateOrder("1", 1, 1, OrderStatus.Active),
                CreateOrder("3", 3.1m, 3.1m, OrderStatus.Active),
                CreateOrder("4", 4, 4, OrderStatus.Active)
            };

            _ordersHistory = new List<OrderHistory>
            {
                CreateOrderHistory("1", 1, 1, OrderStatus.Active),
                CreateOrderHistory("2", 2, 2, OrderStatus.Executed),
                CreateOrderHistory("3", 3, 3, OrderStatus.Active),
                CreateOrderHistory("4", 4, 4, OrderStatus.Active)
            };

            // act

            var result = await _service.ValidateCurrentStateAsync();

            // assert

            Assert.AreEqual(result.Orders.Inconsistent.Count, 1);
        }

        [Test]
        public async Task Restored_Positions_State_Equals_To_Current_State()
        {
            // arrange

            _tradingEngineSnapshot.Positions = new[]
            {
                CreateSnapshotPosition("1", 1),
                CreateSnapshotPosition("2", 2),
                CreateSnapshotPosition("3", 3)
            }.ToJson();

            _currentPositions = new List<Position>
            {
                CreatePosition("1", 1),
                CreatePosition("3", 3.1m),
                CreatePosition("4", 4)
            };

            _positionsHistory = new List<IPositionHistory>
            {
                CreatePositionHistory("1", 1, PositionHistoryType.Open),
                CreatePositionHistory("2", 2, PositionHistoryType.Close),
                CreatePositionHistory("3", 3.1m, PositionHistoryType.Open),
                CreatePositionHistory("4", 4, PositionHistoryType.Open)
            };

            // act

            var result = await _service.ValidateCurrentStateAsync();

            // assert

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public async Task Current_State_Has_Extra_Positions()
        {
            // arrange

            _tradingEngineSnapshot.Positions = new[]
            {
                CreateSnapshotPosition("1", 1),
                CreateSnapshotPosition("2", 2),
                CreateSnapshotPosition("3", 3)
            }.ToJson();

            _currentPositions = new List<Position>
            {
                CreatePosition("1", 1),
                CreatePosition("3", 3.1m),
                CreatePosition("4", 4)
            };

            _positionsHistory = new List<IPositionHistory>
            {
                CreatePositionHistory("1", 1, PositionHistoryType.Open),
                CreatePositionHistory("2", 2, PositionHistoryType.Close),
                CreatePositionHistory("3", 3.1m, PositionHistoryType.Open)
            };

            // act

            var result = await _service.ValidateCurrentStateAsync();

            // assert

            Assert.AreEqual(result.Positions.Extra.Count, 1);
        }

        [Test]
        public async Task Current_State_Has_Missed_Positions()
        {
            // arrange

            _tradingEngineSnapshot.Positions = new[]
            {
                CreateSnapshotPosition("1", 1),
                CreateSnapshotPosition("2", 2),
                CreateSnapshotPosition("3", 3)
            }.ToJson();

            _currentPositions = new List<Position>
            {
                CreatePosition("1", 1),
                CreatePosition("3", 3.1m)
            };

            _positionsHistory = new List<IPositionHistory>
            {
                CreatePositionHistory("1", 1, PositionHistoryType.Open),
                CreatePositionHistory("2", 2, PositionHistoryType.Close),
                CreatePositionHistory("3", 3.1m, PositionHistoryType.Open),
                CreatePositionHistory("4", 4, PositionHistoryType.Open)
            };

            // act

            var result = await _service.ValidateCurrentStateAsync();

            // assert

            Assert.AreEqual(result.Positions.Missed.Count, 1);
        }

        [Test]
        public async Task Current_State_Has_Inconsistent_Positions()
        {
            // arrange

            _tradingEngineSnapshot.Positions = new[]
            {
                CreateSnapshotPosition("1", 1),
                CreateSnapshotPosition("2", 2),
                CreateSnapshotPosition("3", 3)
            }.ToJson();

            _currentPositions = new List<Position>
            {
                CreatePosition("1", 1),
                CreatePosition("3", 3.1m),
                CreatePosition("4", 4)
            };

            _positionsHistory = new List<IPositionHistory>
            {
                CreatePositionHistory("1", 1, PositionHistoryType.Open),
                CreatePositionHistory("2", 2, PositionHistoryType.Close),
                CreatePositionHistory("3", 3, PositionHistoryType.Open),
                CreatePositionHistory("4", 4, PositionHistoryType.Open)
            };

            // act

            var result = await _service.ValidateCurrentStateAsync();

            // assert

            Assert.AreEqual(result.Positions.Inconsistent.Count, 1);
        }

        private static OrderContract CreateSnapshotOrder(string id, decimal volume, decimal? price,
            OrderStatusContract status)
            => new OrderContract {Id = id, Volume = volume, ExpectedOpenPrice = price, Status = status};

        private static Order CreateOrder(string id, decimal volume, decimal? price, OrderStatus status)
            => new Order(id, 0, string.Empty, volume, DateTime.Now, DateTime.Now, null, string.Empty, string.Empty,
                string.Empty, price, string.Empty, OrderFillType.FillOrKill, string.Empty, string.Empty, true,
                OrderType.Market, string.Empty, string.Empty, OriginatorType.Investor, 0, 0, string.Empty,
                FxToAssetPairDirection.Reverse, status, string.Empty, string.Empty);

        private static OrderHistory CreateOrderHistory(string id, decimal volume, decimal? price, OrderStatus status)
            => new OrderHistory {Id = id, Volume = volume, ExpectedOpenPrice = price, Status = status};

        private static PositionContract CreateSnapshotPosition(string id, decimal volume)
            => new PositionContract {Id = id, Volume = volume};

        private static Position CreatePosition(string id, decimal volume)
            => new Position(id, 0, string.Empty, volume, string.Empty, string.Empty, string.Empty, 0, string.Empty,
                DateTime.Now, string.Empty, OrderType.Limit, 0, 0, 0, string.Empty, 0, new List<RelatedOrderInfo>(),
                string.Empty, OriginatorType.Investor, string.Empty, string.Empty, FxToAssetPairDirection.Reverse,
                string.Empty);

        private static PositionHistory CreatePositionHistory(string id, decimal volume, PositionHistoryType historyType)
            => new PositionHistory {Id = id, Volume = volume, HistoryType = historyType};
    }
}