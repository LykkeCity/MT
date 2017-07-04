using Autofac;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Logs;
using MarginTrading.Common.Wamp;
using MarginTrading.Public.Services;
using MarginTrading.Public.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using WampSharp.V2;
using WampSharp.V2.Realm;

namespace MarginTrading.Public.Modules
{
    public class PublicApiModule : Module
    {
        private readonly MtPublicBaseSettings _settings;

        public PublicApiModule(MtPublicBaseSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            LykkeLogToAzureStorage log = new LykkeLogToAzureStorage(PlatformServices.Default.Application.ApplicationName, 
                new AzureTableStorage<LogEntity>(_settings.Db.LogsConnString, "MarginTradingPublicLog", null));

            builder.RegisterInstance((ILog)log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterInstance(log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterInstance(_settings)
                .SingleInstance();

            builder.RegisterType<PricesCacheService>()
                .As<IPricesCacheService>()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<PricesWampService>()
                .As<IStartable>()
                .SingleInstance();
        }
    }
}
