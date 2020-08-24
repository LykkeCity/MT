// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.MarginTrading.OrderBookService.Contracts;
using Lykke.MarginTrading.OrderBookService.Contracts.Models;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.Backend.Core;
using MarginTrading.AssetService.Contracts;
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
            var tradingConditions = MarginTradingTestsUtils.GetPopulatedTradingConditions();
            var tradingInstruments = MarginTradingTestsUtils.GetPopulatedTradingInstruments();
            var meRoutes = MarginTradingTestsUtils.GetPopulatedMatchingEngineRoutes();
            var accountApi = MarginTradingTestsUtils.GetPopulatedAccountsApi(_accounts);

            builder.RegisterInstance(new LogToMemory()).As<ILog>();
            builder.RegisterInstance(assets).As<IAssetsApi>().SingleInstance();
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