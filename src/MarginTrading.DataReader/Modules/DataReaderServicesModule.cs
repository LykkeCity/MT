using System;
using Autofac;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.DataReader.Middleware.Validator;
using MarginTrading.DataReader.Services;
using MarginTrading.DataReader.Services.Implementation;
using MarginTrading.Services;
using AccountAssetsCacheService = MarginTrading.DataReader.Services.Implementation.AccountAssetsCacheService;

namespace MarginTrading.DataReader.Modules
{
    public class DataReaderServicesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ApiKeyValidator>().As<IApiKeyValidator>().SingleInstance();
            builder.RegisterType<OrderBookSnapshotReaderService>().As<IOrderBookSnapshotReaderService>()
                .SingleInstance();
            builder.RegisterType<OrdersSnapshotReaderService>().As<IOrdersSnapshotReaderService>()
                .SingleInstance();
            builder.RegisterInstance(new ConsoleLWriter(Console.WriteLine)).As<IConsole>().SingleInstance();
            builder.RegisterType<MarginTradingOperationsLogService>().As<IMarginTradingOperationsLogService>()
                .SingleInstance();
            builder.RegisterType<QuotesSnapshotReaderService>().As<IQuoteCacheService>()
                .SingleInstance();
            builder.RegisterType<AccountAssetsCacheService>().As<IAccountAssetsCacheService>()
                .SingleInstance();

        }
    }
}