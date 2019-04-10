using Autofac;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.Caches;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Backend.Services.Quotes;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services.Modules
{
    public class ManagersModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AccountManager>()
                .AsSelf()
                .SingleInstance()
                .OnActivated(args => args.Instance.Start());

            builder.RegisterType<OrderCacheManager>()
                .AsSelf()
                .SingleInstance()
                .OnActivated(args => args.Instance.Start());

            builder.RegisterType<TradingConditionsManager>()
                .AsSelf()
                .As<IStartable>()
                .As<ITradingConditionsManager>()
                .SingleInstance();

            builder.RegisterType<TradingInstrumentsManager>()
                .AsSelf()
                .As<ITradingInstrumentsManager>()
                .SingleInstance()
                .OnActivated(args => args.Instance.Start());

            builder.RegisterType<MatchingEngineRoutesManager>()
                .AsSelf()
                .As<IStartable>()
                .As<IMatchingEngineRoutesManager>()
                .SingleInstance();

            builder.RegisterType<AssetPairsManager>()
                .AsSelf()
                .As<IStartable>()
                .As<IAssetPairsManager>()
                .SingleInstance();
            
            builder.RegisterType<AssetsManager>()
                .AsSelf()
                .As<IStartable>()
                .As<IAssetsManager>()
                .SingleInstance();
            
            builder.RegisterType<PendingOrdersCleaningService>()
                .AsSelf()
                .SingleInstance()
                .OnActivated(args => args.Instance.Start());
            
            builder.RegisterType<QuotesMonitor>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();
        }
    }
}
