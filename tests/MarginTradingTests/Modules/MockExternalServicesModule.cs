using System.Collections.Generic;
using Autofac;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AzureRepositories;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.AzureRepositories.Logs;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.SettingsService.Contracts;
using Moq;

namespace MarginTradingTests.Modules
{
    public class MockExternalServicesModule : Module
    {
        private readonly List<MarginTradingAccount> _accounts;

        public MockExternalServicesModule(List<MarginTradingAccount> accounts)
        {
            _accounts = accounts;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var assets = MarginTradingTestsUtils.GetPopulatedAssets();
            var assetPairs = MarginTradingTestsUtils.GetPopulatedAssetPairs();
            
            var accountRepository = MarginTradingTestsUtils.GetPopulatedAccountsRepository(_accounts);
            var conditionsRepository = MarginTradingTestsUtils.GetPopulatedMarginTradingConditionsRepository();
            var accountGroupRepository = MarginTradingTestsUtils.GetPopulatedAccountGroupRepository();
            var accountAssetsRepository = MarginTradingTestsUtils.GetPopulatedAccountAssetsRepository();
            var meRoutesRepository = MarginTradingTestsUtils.GetPopulatedMatchingEngineRoutesRepository();


            builder.RegisterInstance(new LogToMemory()).As<ILog>();
            builder.RegisterInstance(assets).As<IAssetsApi>().SingleInstance();
            builder.RegisterInstance(assetPairs).As<IAssetPairsApi>().SingleInstance();
            
            builder.RegisterInstance(accountRepository).As<IMarginTradingAccountsRepository>().SingleInstance();
            builder.RegisterInstance(conditionsRepository).As<ITradingConditionRepository>().SingleInstance();
            builder.RegisterInstance(accountGroupRepository).As<IAccountGroupRepository>()
                .SingleInstance();
            builder.RegisterInstance(accountAssetsRepository).As<IAccountAssetPairsRepository>().SingleInstance();
            builder.RegisterInstance(meRoutesRepository).As<IMatchingEngineRoutesRepository>().SingleInstance();
            
        }
    }
}