using System;
using Autofac;
using Common.Log;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Common.Settings;
using MarginTrading.DataReader.Middleware.Validator;
using MarginTrading.DataReader.Services;
using MarginTrading.DataReader.Services.Implementation;
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
            builder.RegisterType<RabbitMqService>().As<IRabbitMqService>()
                .SingleInstance();
            builder.RegisterInstance(new ConsoleLWriter(Console.WriteLine)).As<IConsole>()
                .SingleInstance();
        }
    }
}