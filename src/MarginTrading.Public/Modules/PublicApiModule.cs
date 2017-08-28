using Autofac;
using MarginTrading.Public.Services;
using MarginTrading.Public.Settings;

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
            builder.RegisterInstance(_settings)
                .SingleInstance();

            builder.RegisterType<PricesCacheService>()
                .As<IPricesCacheService>()
                .As<IStartable>()
                .SingleInstance();
        }
    }
}
