using System.Collections.Generic;
using Autofac;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.SettingsService.Contracts;

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
            var tradingConditions = MarginTradingTestsUtils.GetPopulatedTradingConditions();
            var tradingInstruments = MarginTradingTestsUtils.GetPopulatedTradingInstruments();
            
            var accountRepository = MarginTradingTestsUtils.GetPopulatedAccountsRepository(_accounts);
            var meRoutesRepository = MarginTradingTestsUtils.GetPopulatedMatchingEngineRoutesRepository();


            builder.RegisterInstance(new LogToMemory()).As<ILog>();
            builder.RegisterInstance(assets).As<IAssetsApi>().SingleInstance();
            builder.RegisterInstance(assetPairs).As<IAssetPairsApi>().SingleInstance();
            builder.RegisterInstance(tradingConditions).As<ITradingConditionsApi>().SingleInstance();
            builder.RegisterInstance(tradingInstruments).As<ITradingInstrumentsApi>().SingleInstance();
            
            builder.RegisterInstance(accountRepository).As<IMarginTradingAccountsRepository>().SingleInstance();
            builder.RegisterInstance(meRoutesRepository).As<IMatchingEngineRoutesRepository>().SingleInstance();
            
        }
    }
}