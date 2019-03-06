using System;
using System.Collections.Generic;
using Autofac;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Services.MatchingEngines;
using Moq;

namespace MarginTradingTests.Modules
{
    public class MockRepositoriesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var blobRepository = new Mock<IMarginTradingBlobRepository>();
            var orderHistoryRepository = new Mock<IOrdersHistoryRepository>();
            var riskSystemCommandsLogRepository = new Mock<IRiskSystemCommandsLogRepository>();
            var accountMarginFreezingRepository = new Mock<IAccountMarginFreezingRepository>();
            var accountMarginUnconfirmedRepository = new Mock<IAccountMarginUnconfirmedRepository>();
            var operationExecutionInfoRepositoryMock = new Mock<IOperationExecutionInfoRepository>();
            operationExecutionInfoRepositoryMock.Setup(s => s.GetOrAddAsync(It.IsIn("AccountsProjection"),
                    It.IsAny<string>(), It.IsAny<Func<IOperationExecutionInfo<OperationData>>>()))
                .ReturnsAsync(new OperationExecutionInfo<OperationData>(
                    operationName: "AccountsProjection",
                    id: Guid.NewGuid().ToString(),
                    lastModified: DateTime.UtcNow,
                    data: new OperationData {State = OperationState.Initiated}
                ));
            var overnightMarginRepositoryMock = new Mock<IOvernightMarginRepository>();

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
            builder.RegisterInstance(riskSystemCommandsLogRepository.Object).As<IRiskSystemCommandsLogRepository>()
                .SingleInstance();

            builder.RegisterInstance(accountMarginFreezingRepository.Object).As<IAccountMarginFreezingRepository>()
                .SingleInstance();
            builder.RegisterInstance(accountMarginUnconfirmedRepository.Object).As<IAccountMarginUnconfirmedRepository>()
                .SingleInstance();
            builder.RegisterInstance(operationExecutionInfoRepositoryMock.Object)
                .As<IOperationExecutionInfoRepository>().SingleInstance();
            builder.RegisterInstance(overnightMarginRepositoryMock.Object).As<IOvernightMarginRepository>()
                .SingleInstance();
        }
    }
}