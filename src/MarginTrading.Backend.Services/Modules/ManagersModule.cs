using Autofac;
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
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<OrderCacheManager>()
                .AsSelf()
                .SingleInstance()
                .OnActivated(args => args.Instance.Start());

            builder.RegisterType<TradingConditionsManager>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<AccountAssetsManager>()
                .AsSelf()
                .SingleInstance()
                .OnActivated(args => args.Instance.Start());

            builder.RegisterType<AccountGroupManager>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<MicrographManager>()
                .AsSelf()
                .SingleInstance()
                .OnActivated(args => args.Instance.Start());

            builder.RegisterType<MatchingEngineRoutesManager>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();

            builder.RegisterType<InstrumentsManager>()
                .AsSelf()
                .As<IStartable>()
                .SingleInstance();
            
            builder.RegisterType<PendingOrdersCleaningService>()
                .AsSelf()
                .SingleInstance()
                .OnActivated(args => args.Instance.Start());
        }
    }
}
