using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.Caches;
using Rocks.Caching;

namespace MarginTrading.Backend.Services.Modules
{
    public class CacheModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AssetPairsCache>()
                .As<IAssetPairsCache>()
                .As<IAssetPairsInitializableCache>()
                .AsSelf()
                .SingleInstance();
            
            builder.RegisterType<AssetsCache>()
                .As<IAssetsCache>()
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

            builder.RegisterType<OvernightSwapCache>()
                .As<IOvernightSwapCache>()
                .SingleInstance();

            builder.RegisterType<MemoryCacheProvider>()
                   .As<ICacheProvider>()
                   .AsSelf()
                   .SingleInstance();
        }
    }
}
