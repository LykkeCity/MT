// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.MarginTrading.OrderBookService.Contracts;
using Lykke.MarginTrading.OrderBookService.Contracts.Models;
using Lykke.Snow.Mdm.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.Backend.Core;
using MarginTrading.AssetService.Contracts;
using Microsoft.FeatureManagement;
using Moq;

namespace MarginTradingTests.Modules
{
    public class MockExternalServicesModule : Module
    {
        private readonly List<MarginTradingAccount> _accounts;
        private readonly string _brokerId;

        public MockExternalServicesModule(List<MarginTradingAccount> accounts, string brokerId)
        {
            _accounts = accounts;
            _brokerId = brokerId;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var assets = MarginTradingTestsUtils.GetPopulatedAssets();
            var assetPairs = MarginTradingTestsUtils.GetPopulatedAssetPairs();
            var tradingConditions = MarginTradingTestsUtils.GetPopulatedTradingConditions();
            var tradingInstruments = MarginTradingTestsUtils.GetPopulatedTradingInstruments();
            var meRoutes = MarginTradingTestsUtils.GetPopulatedMatchingEngineRoutes();
            var accountApi = MarginTradingTestsUtils.GetPopulatedAccountsApi(_accounts);
            var brokerSettingsApi = MarginTradingTestsUtils.GetBrokerSettingsApi(_brokerId);
            var featureManager = MarginTradingTestsUtils.GetFeatureManager(_brokerId, brokerSettingsApi);
            
            builder.RegisterInstance(new LogToMemory()).As<ILog>();
            builder.RegisterInstance(assets).As<IAssetsApi>().SingleInstance();
            builder.RegisterInstance(brokerSettingsApi).As<IBrokerSettingsApi>().SingleInstance();
            builder.RegisterInstance(featureManager).As<IFeatureManager>().SingleInstance();
            builder.RegisterInstance(assetPairs).As<IAssetPairsApi>().SingleInstance();
            builder.RegisterInstance(tradingConditions).As<ITradingConditionsApi>().SingleInstance();
            builder.RegisterInstance(tradingInstruments).As<ITradingInstrumentsApi>().SingleInstance();
            builder.RegisterInstance(meRoutes).As<ITradingRoutesApi>().SingleInstance();
            builder.RegisterInstance(accountApi).As<IAccountsApi>().SingleInstance();
            builder.RegisterInstance(Mock.Of<IOrderBookProviderApi>(x =>
                x.GetOrderBooks(null) == Task.FromResult(new List<ExternalOrderBookContract>())));
        }
    }
}