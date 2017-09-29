using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Reports;
using MarginTrading.Core;
using MarginTrading.Core.Clients;
using MarginTrading.Services;
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
            var assetsRepository = MarginTradingTestsUtils.GetPopulatedAssetsRepository();
            var accountRepository = MarginTradingTestsUtils.GetPopulatedAccountsRepository(_accounts);
            var conditionsRepository = MarginTradingTestsUtils.GetPopulatedMarginTradingConditionsRepository();
            var accountGroupRepository = MarginTradingTestsUtils.GetPopulatedAccountGroupRepository();
            var accountAssetsRepository = MarginTradingTestsUtils.GetPopulatedAccountAssetsRepository();
            var watchListRepository = MarginTradingTestsUtils.GetPopulatedMarginTradingWatchListsRepository();
            var meRoutesRepository = MarginTradingTestsUtils.GetPopulatedMatchingEngineRoutesRepository();

            var blobRepository = new Mock<IMarginTradingBlobRepository>();
            var orderHistoryRepository = new Mock<IMarginTradingOrdersHistoryRepository>();
            var clientAccountsRepository = new Mock<IClientAccountsRepository>();
            var accountsStatsReportsRepository = new Mock<IAccountsStatsReportsRepository>();
            var accountsReportsRepository = new Mock<IAccountsReportsRepository>();
            clientAccountsRepository
                .Setup(item => item.GetByIdAsync(It.IsAny<string>()))
                .Returns(() =>
                    Task.FromResult(
                        (IClientAccount) new ClientAccount {Id = "1", NotificationsId = new Guid().ToString()}));

            var clientSettingsRepository = new Mock<IClientSettingsRepository>();
            clientSettingsRepository
                .Setup(item => item.GetSettings<PushNotificationsSettings>(It.IsAny<string>()))
                .Returns(() => Task.FromResult(new PushNotificationsSettings {Enabled = true}));

            builder.RegisterInstance(new LogToMemory()).As<ILog>();
            builder.RegisterInstance(assetsRepository).As<IAssetPairsRepository>().SingleInstance();
            builder.RegisterInstance(accountRepository).As<IMarginTradingAccountsRepository>().SingleInstance();
            builder.RegisterInstance(
                    new MarginTradingAccountStatsRepository(new NoSqlTableInMemory<MarginTradingAccountStatsEntity>()))
                .As<IMarginTradingAccountStatsRepository>().SingleInstance();
            builder.RegisterInstance(conditionsRepository).As<IMarginTradingConditionRepository>().SingleInstance();
            builder.RegisterInstance(accountGroupRepository).As<IMarginTradingAccountGroupRepository>()
                .SingleInstance();
            builder.RegisterInstance(accountAssetsRepository).As<IAccountAssetPairsRepository>().SingleInstance();
            builder.RegisterInstance(watchListRepository).As<IMarginTradingWatchListRepository>().SingleInstance();
            builder.RegisterInstance(meRoutesRepository).As<IMatchingEngineRoutesRepository>().SingleInstance();
            builder.RegisterType<MatchingEngineInMemoryRepository>().As<IMatchingEngineRepository>().SingleInstance();

            //mocks
            builder.RegisterInstance(blobRepository.Object).As<IMarginTradingBlobRepository>().SingleInstance();
            builder.RegisterInstance(orderHistoryRepository.Object).As<IMarginTradingOrdersHistoryRepository>()
                .SingleInstance();
            builder.RegisterInstance(clientSettingsRepository.Object).As<IClientSettingsRepository>().SingleInstance();
            builder.RegisterInstance(clientAccountsRepository.Object).As<IClientAccountsRepository>().SingleInstance();
            builder.RegisterInstance(accountsStatsReportsRepository.Object).As<IAccountsStatsReportsRepository>().SingleInstance();
            builder.RegisterInstance(accountsReportsRepository.Object).As<IAccountsReportsRepository>().SingleInstance();
        }
    }
}