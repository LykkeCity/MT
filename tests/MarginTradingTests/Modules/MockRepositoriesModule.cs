using Autofac;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
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
            builder.Register<IDayOffSettingsRepository>(c => new DayOffSettingsRepository(blobRepository.Object))
                .SingleInstance();
        }
    }
}