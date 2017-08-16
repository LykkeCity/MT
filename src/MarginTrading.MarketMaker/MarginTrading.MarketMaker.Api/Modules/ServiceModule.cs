using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using MarginTrading.MarketMaker.Api.Core;
using MarginTrading.MarketMaker.Api.Core.Services;
using MarginTrading.MarketMaker.Api.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.MarketMaker.Api.Modules
{
    public class ServiceModule : Module
    {
        private readonly MarginTrading_MarketMaker_ApiSettings _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public ServiceModule(MarginTrading_MarketMaker_ApiSettings settings, ILog log)
        {
            _settings = settings;
            _log = log;

            _services = new ServiceCollection();
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings)
                .SingleInstance();

            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            // TODO: Add your dependencies here

            builder.Populate(_services);
        }
    }
}
