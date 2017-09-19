using Autofac;
using Common.Log;
using MarginTrading.MarketMaker.AzureRepositories;
using MarginTrading.MarketMaker.AzureRepositories.Implementation;
using MarginTrading.MarketMaker.HelperServices;
using MarginTrading.MarketMaker.Services;
using MarginTrading.MarketMaker.Settings;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using Rocks.Caching;
using MarginTrading.MarketMaker.Services.Implementation;

namespace MarginTrading.MarketMaker.Modules
{
    internal class MarketMakerModule : Module
    {
        private readonly AppSettings _settings;
        private readonly ILog _log;

        public MarketMakerModule(AppSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.MarginTradingMarketMaker).SingleInstance();
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();
            builder.RegisterType<AssetsPairsSettingsRepository>().As<IAssetsPairsSettingsRepository>().SingleInstance();
            builder.RegisterType<MarketMakerService>().As<IMarketMakerService>().SingleInstance();
            builder.RegisterType<MemoryCacheProvider>().As<ICacheProvider>().SingleInstance();
            builder.RegisterType<RabbitMqService>().As<IRabbitMqService>().SingleInstance();
            builder.RegisterType<AssetPairsSettingsService>().As<IAssetPairsSettingsService>().SingleInstance();
            builder.RegisterType<SystemService>().As<ISystem>().SingleInstance();
            builder.RegisterType<BrokerService>().As<IBrokerService>().InstancePerDependency();
        }
    }
}