using Autofac;
using Lykke.SettingsReader;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Settings;

namespace MarginTrading.Backend.Modules
{
    public class BackendSettingsModule : Module
    {
        private readonly MtBackendSettings _mtSettings;
        private readonly IReloadingManager<MarginSettings> _settings;

        public BackendSettingsModule(MtBackendSettings mtSettings, IReloadingManager<MarginSettings> settings)
        {
            _mtSettings = mtSettings;
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_mtSettings.EmailSender).SingleInstance();
            builder.RegisterInstance(_mtSettings.Jobs).SingleInstance();
            builder.RegisterInstance(_settings).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue.RequestLoggerSettings).SingleInstance();
        }
    }
}
