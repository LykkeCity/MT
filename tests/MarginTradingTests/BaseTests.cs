using System;
using System.Collections.Generic;
using Autofac;
using MarginTrading.Core;
using MarginTrading.Core.MatchingEngines;
using MarginTrading.Core.Models;
using MarginTrading.Core.Settings;
using MarginTrading.Frontend.Services;
using MarginTrading.Services;
using MarginTrading.Services.Events;
using MarginTrading.Services.MatchingEngines;
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

            builder.RegisterModule(new ManagersModule());
            builder.RegisterModule(new CacheModule());
            builder.RegisterModule(new ServicesModule());
            builder.RegisterModule(new ManagersModule());

            builder.RegisterType<WatchListService>()
                .As<IWatchListService>()
                .SingleInstance();

            builder.RegisterType<UpdatedAccountsTrackingService>()
                .As<IUpdatedAccountsTrackingService>()
                .SingleInstance();

            builder.RegisterType<EventChannel<AccountBalanceChangedEventArgs>>()
                .As<IEventChannel<AccountBalanceChangedEventArgs>>()
                .SingleInstance();

            var settingsServiceMock = new Mock<IMarginTradingSettingsService>();
            settingsServiceMock.Setup(s => s.IsMarginTradingEnabled(It.IsAny<string>())).ReturnsAsync(new EnabledMarginTradingTypes { Live = true, Demo = true });
            settingsServiceMock.Setup(s => s.IsMarginTradingEnabled(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);


            builder.RegisterInstance(settingsServiceMock.Object)
                .As<IMarginTradingSettingsService>()
                .SingleInstance();

            var settings = new ScheduleSettings
            {
                DayOffStartDay = DayOfWeek.Sunday,
                DayOffStartTime = new TimeSpan(21, 0, 0),
                DayOffEndDay = DayOfWeek.Sunday,
                DayOffEndTime = new TimeSpan(21, 0, 0),
                AssetPairsWithoutDayOff = new[] {"BTCCHF"}
            };

            builder.RegisterInstance(new Mock<IMarginTradingOperationsLogService>().Object)
                .As<IMarginTradingOperationsLogService>()
                .SingleInstance();
            builder.RegisterInstance(settings).SingleInstance();

            builder.RegisterBuildCallback(c => c.Resolve<AccountAssetsManager>());
            builder.RegisterBuildCallback(c => c.Resolve<OrderCacheManager>());

            Container = builder.Build();

            var meRepository = Container.Resolve<IMatchingEngineRepository>();
            meRepository.InitMatchingEngines(new List<IMatchingEngineBase> {
                Container.Resolve<IInternalMatchingEngine>(),
                new RejectMatchingEngine()
            });

            MtServiceLocator.FplService = Container.Resolve<IFplService>();
            MtServiceLocator.AccountUpdateService = Container.Resolve<IAccountUpdateService>();
            MtServiceLocator.AccountsCacheService = Container.Resolve<IAccountsCacheService>();
            MtServiceLocator.SwapCommissionService = Container.Resolve<ICommissionService>();

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
