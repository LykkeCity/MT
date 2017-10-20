using System;
using System.Linq;
using Autofac;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.MarketMaker.HelperServices;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Services;
using MarginTrading.MarketMaker.Services.Implementation;
using MarginTrading.MarketMaker.Settings;
using Rocks.Caching;

namespace MarginTrading.MarketMaker.Modules
{
    internal class MarketMakerModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;
        private readonly ILog _log;

        public MarketMakerModule(IReloadingManager<AppSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            RegisterDefaultImplementions(builder);

            builder.RegisterInstance(_settings.Nested(s => s.MarginTradingMarketMaker)).SingleInstance();
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();
            builder.RegisterType<SystemService>().As<ISystem>().SingleInstance();
            builder.RegisterType<MemoryCacheProvider>().As<ICacheProvider>().SingleInstance();

            builder.RegisterInstance(new RabbitMqService(_log,
                    _settings.Nested(s => s.MarginTradingMarketMaker.Db.QueuePersistanceRepositoryConnString)))
                .As<IRabbitMqService>().SingleInstance();

            builder.RegisterType<BrokerService>().As<IBrokerService>().InstancePerDependency();
        }

        /// <summary>
        /// Scans for types in the current assembly and registers types which: <br/>
        /// - are named like 'SmthService' <br/>
        /// - implement an non-generic interface named like 'ISmthService' in the same assembly <br/>
        /// - are the only implementations of the 'ISmthService' interface <br/>
        /// - are not generic <br/><br/>
        /// Types like SmthRepository are also supported.
        /// </summary>
        private void RegisterDefaultImplementions(ContainerBuilder builder)
        {
            var assembly = GetType().Assembly;
            var implementations = assembly.GetTypes()
                .Where(t => !t.IsInterface && !t.IsGenericType && (t.Name.EndsWith("Service") || t.Name.EndsWith("Repository")))
                .SelectMany(t =>
                    t.GetInterfaces()
                        .Where(i => i.Name.StartsWith('I') && i.Name.Substring(1) == t.Name && t.Assembly == assembly || t == typeof(IStartable))
                        .Select(i => (Implementation: t, Interface: i)))
                .GroupBy(t => t.Interface)
                .Where(gr => gr.Count() == 1)
                .Select(gr => gr.First());

            foreach (var (impl, service) in implementations)
            {
                builder.RegisterType(impl).As(service).SingleInstance();
            }
        }
    }
}