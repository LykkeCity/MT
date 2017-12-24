using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Autofac;
using MarginTrading.AzureRepositories;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Events;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Modules;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Services;
using MarginTrading.Common.Settings;
using MarginTrading.Common.Settings.Models;
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

            builder.RegisterType<EventChannel<AccountBalanceChangedEventArgs>>()
                .As<IEventChannel<AccountBalanceChangedEventArgs>>()
                .SingleInstance();

            var settingsServiceMock = new Mock<IMarginTradingSettingsService>();
            settingsServiceMock.Setup(s => s.IsMarginTradingEnabled(It.IsAny<string>())).ReturnsAsync(new EnabledMarginTradingTypes { Live = true, Demo = true });
            settingsServiceMock.Setup(s => s.IsMarginTradingEnabled(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(true);


            builder.RegisterInstance(settingsServiceMock.Object)
                .As<IMarginTradingSettingsService>()
                .SingleInstance();


            builder.RegisterInstance(new Mock<IMarginTradingOperationsLogService>().Object)
                .As<IMarginTradingOperationsLogService>()
                .SingleInstance();
            
            var settings = new ScheduleSettings(
                dayOffStartDay: DayOfWeek.Sunday,
                dayOffStartTime: new TimeSpan(21, 0, 0),
                dayOffEndDay: DayOfWeek.Sunday,
                dayOffEndTime: new TimeSpan(21, 0, 0),
                assetPairsWithoutDayOff: new[] {"BTCCHF"}.ToHashSet(),
                pendingOrdersCutOff: TimeSpan.Zero);
            var dayOffSettingsService = new Mock<IDayOffSettingsService>(MockBehavior.Strict);
            dayOffSettingsService.Setup(s => s.GetScheduleSettings()).Returns(settings);
            dayOffSettingsService.Setup(s => s.GetExclusions(It.IsNotNull<string>())).Returns(ImmutableArray<DayOffExclusion>.Empty);
            builder.RegisterInstance(dayOffSettingsService.Object).SingleInstance();
            builder.Register<IDayOffSettingsRepository>(c => new DayOffSettingsRepository(c.Resolve<IMarginTradingBlobRepository>())).SingleInstance();

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
