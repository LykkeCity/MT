using Autofac;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Frontend.Services;
using MarginTrading.Frontend.Tests.Modules;
using Moq;

namespace MarginTrading.Frontend.Tests
{
    public class BaseTests
    {
        protected IContainer Container { get; set; }

        protected void RegisterDependencies(bool mockEvents = false)
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule(new MockBaseServicesModule());
            builder.RegisterModule(new MockRepositoriesModule());

            builder.RegisterType<WatchListService>()
                .As<IWatchListService>()
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
            
            builder.RegisterType<ClientTokenService>()
                .As<IClientTokenService>()
                .SingleInstance();

            Container = builder.Build();
        }
    }
}
