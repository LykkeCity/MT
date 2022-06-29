// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Autofac;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.MatchingEngines;
using Moq;
using StackExchange.Redis;
using Order = MarginTrading.Backend.Core.Trading.Order;

namespace MarginTradingTests.Modules
{
    public class MockRepositoriesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var blobRepository = new Mock<IMarginTradingBlobRepository>();
            blobRepository.Setup(s => s.ReadWithTimestampAsync<List<Order>>(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((new List<Order>(), DateTime.UtcNow));
            blobRepository.Setup(s => s.ReadWithTimestampAsync<List<Position>>(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((new List<Position>(), DateTime.UtcNow));
            var orderHistoryRepository = new Mock<IOrdersHistoryRepository>();
            orderHistoryRepository.Setup(s => s.GetLastSnapshot(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<IOrderHistory>());
            var positionHistoryRepository = new Mock<IPositionsHistoryRepository>();
            var accountHistoryRepository = new Mock<IAccountHistoryRepository>();
            positionHistoryRepository.Setup(s => s.GetLastSnapshot(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<IPositionHistory>());
            accountHistoryRepository.Setup(s => s.GetSwapTotalPerPosition(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());
            var riskSystemCommandsLogRepository = new Mock<IRiskSystemCommandsLogRepository>();
            var accountMarginFreezingRepository = new Mock<IAccountMarginFreezingRepository>();
            var accountMarginUnconfirmedRepository = new Mock<IAccountMarginUnconfirmedRepository>();
            var operationExecutionInfoRepositoryMock = new Mock<IOperationExecutionInfoRepository>();
            var snapshotsRepository = new Mock<ITradingEngineSnapshotsRepository>();
            var connectionMultiplexer = new Mock<IConnectionMultiplexer>();
            
            operationExecutionInfoRepositoryMock.Setup(s => s.GetOrAddAsync(It.IsIn("AccountsProjection"),
                    It.IsAny<string>(), It.IsAny<Func<IOperationExecutionInfo<OperationData>>>()))
                .ReturnsAsync((new OperationExecutionInfo<OperationData>(
                    operationName: "AccountsProjection",
                    id: Guid.NewGuid().ToString(),
                    lastModified: DateTime.UtcNow,
                    data: new OperationData {State = OperationState.Initiated}
                ), true));

            accountMarginFreezingRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<IAccountMarginFreezing>().AsReadOnly());
            accountMarginUnconfirmedRepository.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<IAccountMarginFreezing>().AsReadOnly());

            builder.RegisterInstance(new LogToMemory()).As<ILog>();
            builder.RegisterInstance(
                    new MarginTradingAccountStatsRepository(new NoSqlTableInMemory<MarginTradingAccountStatsEntity>()))
                .As<IMarginTradingAccountStatsRepository>().SingleInstance();
            builder.RegisterType<MatchingEngineInMemoryRepository>().As<IMatchingEngineRepository>().SingleInstance();

            //mocks
            builder.RegisterInstance(blobRepository.Object).As<IMarginTradingBlobRepository>().SingleInstance();
            builder.RegisterInstance(orderHistoryRepository.Object).As<IOrdersHistoryRepository>()
                .SingleInstance();
            builder.RegisterInstance(positionHistoryRepository.Object).As<IPositionsHistoryRepository>()
                .SingleInstance();
            builder.RegisterInstance(accountHistoryRepository.Object).As<IAccountHistoryRepository>()
                .SingleInstance();
            builder.RegisterInstance(riskSystemCommandsLogRepository.Object).As<IRiskSystemCommandsLogRepository>()
                .SingleInstance();
            builder.RegisterInstance(accountMarginFreezingRepository.Object).As<IAccountMarginFreezingRepository>()
                .SingleInstance();
            builder.RegisterInstance(accountMarginUnconfirmedRepository.Object).As<IAccountMarginUnconfirmedRepository>()
                .SingleInstance();
            builder.RegisterInstance(operationExecutionInfoRepositoryMock.Object)
                .As<IOperationExecutionInfoRepository>().SingleInstance();
            builder.RegisterInstance(snapshotsRepository.Object)
                .As<ITradingEngineSnapshotsRepository>().SingleInstance();
            builder.RegisterInstance(connectionMultiplexer.Object)
                .As<IConnectionMultiplexer>().SingleInstance();
        }
    }
}