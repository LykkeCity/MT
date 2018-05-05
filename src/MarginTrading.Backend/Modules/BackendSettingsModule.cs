using Autofac;
using Lykke.SettingsReader;
using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.Backend.Modules
{
    public class BackendSettingsModule : Module
    {
        private readonly IReloadingManager<MarginTradingSettings> _settings;

        public BackendSettingsModule(IReloadingManager<MarginTradingSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.RequestLoggerSettings).SingleInstance();
        }
    }
}
