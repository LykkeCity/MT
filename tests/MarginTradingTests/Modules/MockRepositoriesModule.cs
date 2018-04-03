using System.Collections.Generic;
using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Service.Assets.Client;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Contract;
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
        private readonly List<MarginTradingAccount> _accounts;

        public MockRepositoriesModule(List<MarginTradingAccount> accounts)
        {
            _accounts = accounts;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var assetsService = MarginTradingTestsUtils.GetPopulatedAssetsService();
            var accountRepository = MarginTradingTestsUtils.GetPopulatedAccountsRepository(_accounts);
            var conditionsRepository = MarginTradingTestsUtils.GetPopulatedMarginTradingConditionsRepository();
            var accountGroupRepository = MarginTradingTestsUtils.GetPopulatedAccountGroupRepository();
            var accountAssetsRepository = MarginTradingTestsUtils.GetPopulatedAccountAssetsRepository();
            var meRoutesRepository = MarginTradingTestsUtils.GetPopulatedMatchingEngineRoutesRepository();
            var overnightSwapStateRepository = MarginTradingTestsUtils.GetOvernightSwapStateRepository();
            var overnightSwapHistoryRepository = MarginTradingTestsUtils.GetOvernightSwapHistoryRepository();

            var blobRepository = new Mock<IMarginTradingBlobRepository>();
            var orderHistoryRepository = new Mock<IMarginTradingOrdersHistoryRepository>();
            var riskSystemCommandsLogRepository = new Mock<IRiskSystemCommandsLogRepository>();

            builder.RegisterInstance(new LogToMemory()).As<ILog>();
            builder.RegisterInstance(assetsService).As<IAssetsService>().SingleInstance();
            builder.RegisterInstance(accountRepository).As<IMarginTradingAccountsRepository>().SingleInstance();
            builder.RegisterInstance(
                    new MarginTradingAccountStatsRepository(new NoSqlTableInMemory<MarginTradingAccountStatsEntity>()))
                .As<IMarginTradingAccountStatsRepository>().SingleInstance();
            builder.RegisterInstance(conditionsRepository).As<ITradingConditionRepository>().SingleInstance();
            builder.RegisterInstance(accountGroupRepository).As<IAccountGroupRepository>()
                .SingleInstance();
            builder.RegisterInstance(accountAssetsRepository).As<IAccountAssetPairsRepository>().SingleInstance();
            builder.RegisterInstance(meRoutesRepository).As<IMatchingEngineRoutesRepository>().SingleInstance();
            builder.RegisterInstance(overnightSwapStateRepository).As<IOvernightSwapStateRepository>().SingleInstance();
            builder.RegisterInstance(overnightSwapHistoryRepository).As<IOvernightSwapHistoryRepository>()
                .SingleInstance();
            builder.RegisterType<MatchingEngineInMemoryRepository>().As<IMatchingEngineRepository>().SingleInstance();

            //mocks
            builder.RegisterInstance(blobRepository.Object).As<IMarginTradingBlobRepository>().SingleInstance();
            builder.RegisterInstance(orderHistoryRepository.Object).As<IMarginTradingOrdersHistoryRepository>()
                .SingleInstance();
            builder.RegisterInstance(riskSystemCommandsLogRepository.Object).As<IRiskSystemCommandsLogRepository>()
                .SingleInstance();
            builder.Register<IDayOffSettingsRepository>(c => new DayOffSettingsRepository(blobRepository.Object))
                .SingleInstance();
            builder.RegisterInstance(MarginTradingTestsUtils.GetPopulatedAssetPairsRepository())
                .As<IAssetPairsRepository>().SingleInstance();
        }
    }
}