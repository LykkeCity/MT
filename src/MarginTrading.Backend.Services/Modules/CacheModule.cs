// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.AssetPairs;
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
