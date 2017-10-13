using Autofac;
using MarginTrading.Services.MatchingEngines;

namespace MarginTrading.Services.Modules
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
                .As<IStartable>()
                .SingleInstance();

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
        }
    }
}
