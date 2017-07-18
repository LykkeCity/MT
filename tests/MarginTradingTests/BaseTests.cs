﻿using System;
using System.Collections.Generic;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Core.Settings;
using MarginTrading.Frontend.Services;
using MarginTrading.Services;
using MarginTrading.Services.Modules;
using MarginTradingTests.Modules;
using Moq;

namespace MarginTradingTests
{
    public class BaseTests
    {
        private const string ClientId1 = "1";
        private const string ClientId2 = "2";

        protected IContainer Container { get; set; }

        protected void RegisterDependencies(bool mockEvents = false)
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule(new SettingsModule());
            builder.RegisterModule(new MockBaseServicesModule());
            builder.RegisterModule(new MockRepositoriesModule(Accounts));

            if (mockEvents)
            {
                builder.RegisterModule(new MockEventModule());
            }
            else
            {
                builder.RegisterModule(new EventModule());
            }

            builder.RegisterModule(new CacheModule());
            builder.RegisterModule(new ServicesModule());
            builder.RegisterModule(new ManagersModule());

            builder.RegisterType<WatchListService>()
                .As<IWatchListService>()
                .SingleInstance();

            var settings = new MarketMakerSettings
            {
                DayOffStartDay = DayOfWeek.Sunday.ToString(),
                DayOffStartHour = 21,
                DayOffEndDay = DayOfWeek.Sunday.ToString(),
                DayOffEndHour = 21,
                AssetsWithoutDayOff = new[] { "BTCCHF" }
            };

            builder.RegisterInstance(new Mock<IMarginTradingOperationsLogService>().Object)
                .As<IMarginTradingOperationsLogService>()
                .SingleInstance();
            builder.RegisterInstance(settings).SingleInstance();

            builder.RegisterBuildCallback(c => c.Resolve<AccountAssetsManager>());

            Container = builder.Build();

            var meRepository = Container.Resolve<IMatchingEngineRepository>();
            meRepository.InitMatchingEngines(new List<object> {
                Container.Resolve<IMatchingEngine>(),
                new MatchingEngineBase { Id = MatchingEngines.Icm }
            });

            MtServiceLocator.FplService = Container.Resolve<IFplService>();
            MtServiceLocator.AccountUpdateService = Container.Resolve<IAccountUpdateService>();
            MtServiceLocator.AccountsCacheService = Container.Resolve<IAccountsCacheService>();
            MtServiceLocator.SwapCommissionService = Container.Resolve<ISwapCommissionService>();

            Container.Resolve<OrderBookList>().Init(null);
        }

        protected List<MarginTradingAccount> Accounts = new List<MarginTradingAccount>
        {
            new MarginTradingAccount
            {
                Id = Guid.NewGuid().ToString("N"),
                TradingConditionId = "1",
                BaseAssetId = "USD",
                ClientId = ClientId1,
                Balance = 1000
            },
            new MarginTradingAccount
            {
                Id = Guid.NewGuid().ToString("N"),
                TradingConditionId = "1",
                BaseAssetId = "EUR",
                ClientId = ClientId1,
                Balance = 1000
            },
            new MarginTradingAccount
            {
                Id = Guid.NewGuid().ToString("N"),
                TradingConditionId = "1",
                BaseAssetId = "CHF",
                ClientId = ClientId1,
                Balance = 1000
            },

            new MarginTradingAccount
            {
                Id = Guid.NewGuid().ToString("N"),
                TradingConditionId = "1",
                BaseAssetId = "USD",
                ClientId = ClientId2,
                Balance = 1000
            },
            new MarginTradingAccount
            {
                Id = Guid.NewGuid().ToString("N"),
                TradingConditionId = "1",
                BaseAssetId = "EUR",
                ClientId = ClientId2,
                Balance = 1000
            },
            new MarginTradingAccount
            {
                Id = Guid.NewGuid().ToString("N"),
                TradingConditionId = "1",
                BaseAssetId = "CHF",
                ClientId = ClientId2,
                Balance = 1000
            }
        };
    }
}
