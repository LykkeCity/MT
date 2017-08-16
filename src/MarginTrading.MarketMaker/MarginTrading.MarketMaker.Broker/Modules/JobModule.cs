using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using MarginTrading.MarketMaker.Broker.Core;
using MarginTrading.MarketMaker.Broker.Core.Services;
using MarginTrading.MarketMaker.Broker.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.MarketMaker.Broker.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings.MarginTrading_MarketMaker_JobSettings _settings;
        private readonly ILog _log;
        // NOTE: you can remove it if you don't need to use IServiceCollection extensions to register service specific dependencies
        private readonly IServiceCollection _services;

        public JobModule(AppSettings.MarginTrading_MarketMaker_JobSettings settings, ILog log)
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
                .SingleInstance()
                .WithParameter(TypedParameter.From(TimeSpan.FromSeconds(30)));

            // NOTE: Service registrations example:

            builder.RegisterType<MyFooService>()
                .As<IMyFooService>();

            builder.RegisterType<MyBooService>()
                .As<IMyBooService>();

            // NOTE: You can implement your own poison queue notifier. See https://github.com/LykkeCity/JobTriggers/blob/master/readme.md
            // builder.Register<PoisionQueueNotifierImplementation>().As<IPoisionQueueNotifier>();

            // TODO: Add your dependencies here

            builder.Populate(_services);
        }
    }
}