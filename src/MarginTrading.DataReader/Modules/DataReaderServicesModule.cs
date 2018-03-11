using System;
using Autofac;
using Common.Log;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Common.Settings;
using MarginTrading.DataReader.Middleware.Validator;
using MarginTrading.DataReader.Services;
using MarginTrading.DataReader.Services.Implementation;
using MarginTrading.DataReader.Settings;
using Rocks.Caching;

namespace MarginTrading.DataReader.Modules
{
    public class DataReaderServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ApiKeyValidator>().As<IApiKeyValidator>()
                .SingleInstance();
            builder.RegisterType<OrderBookSnapshotReaderService>().As<IOrderBookSnapshotReaderService>()
                .SingleInstance();
            builder.RegisterType<OrdersSnapshotReaderService>().As<IOrdersSnapshotReaderService>()
                .SingleInstance();
            builder.RegisterType<MarginTradingEnabledCacheService>().As<IMarginTradingSettingsCacheService>()
                .SingleInstance();
            builder.RegisterType<MemoryCacheProvider>().As<ICacheProvider>()
                .SingleInstance();
            builder.RegisterType<Application>().As<IStartable>()
                .SingleInstance();
            
            builder.Register(c => new RabbitMqService(c.Resolve<ILog>(), c.Resolve<IConsole>(),
                    queueRepositoryConnectionString: null, // todo: when fault-tolerant producers become needed add a string from config here
                    env: c.Resolve<DataReaderSettings>().Env))
                .As<IRabbitMqService>()
                .SingleInstance();
            
            builder.RegisterInstance(new ConsoleLWriter(Console.WriteLine)).As<IConsole>()
                .SingleInstance();
        }
    }
}