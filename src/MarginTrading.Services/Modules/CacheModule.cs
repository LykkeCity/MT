using Autofac;
using MarginTrading.Core;
using Rocks.Caching;

namespace MarginTrading.Services.Modules
{
    public class CacheModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AssetPairsCache>()
                .As<IAssetPairsCache>()
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

            builder.RegisterType<MemoryCacheProvider>()
                   .As<ICacheProvider>()
                   .AsSelf()
                   .SingleInstance();
        }
    }
}
