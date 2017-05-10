using Autofac;
using MarginTrading.Core;

namespace MarginTrading.Services.Modules
{
    public class CacheModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<InstrumentsCache>()
                .As<IInstrumentsCache>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterType<AccountsCacheService>()
                .AsSelf()
                .As<IAccountsCacheService>()
                .SingleInstance();

            builder.RegisterType<OrdersCache>()
                .As<IOrderReader>()
                .AsSelf()
                .SingleInstance();
        }
    }
}
