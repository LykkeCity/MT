// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Autofac;
using MarginTrading.AssetService.Contracts.ClientProfileSettings;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.AssetPairs;
using Rocks.Caching;
using StackExchange.Redis;

namespace MarginTrading.Backend.Services.Modules
{
    public class CacheModule : Module
    {
        private readonly MarginTradingSettings _marginTradingSettings;

        public CacheModule(MarginTradingSettings marginTradingSettings)
        {
            _marginTradingSettings = marginTradingSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(c => ConnectionMultiplexer.Connect(_marginTradingSettings.RedisSettings.Configuration))
                .As<IConnectionMultiplexer>()
                .SingleInstance();
            
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

            builder.RegisterType<ClientProfileSettingsCache>()
                .As<IClientProfileSettingsCache>()
                .AsSelf()
                .SingleInstance();
        }
    }
}
