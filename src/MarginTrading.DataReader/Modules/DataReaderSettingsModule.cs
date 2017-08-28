using Autofac;
using MarginTrading.Core.Settings;
using MarginTrading.Services.Settings;

namespace MarginTrading.DataReader.Modules
{
    public class DataReaderSettingsModule : Module
    {
        private readonly MtBackendSettings _mtSettings;
        private readonly MarginSettings _settings;

        public DataReaderSettingsModule(MtBackendSettings mtSettings, MarginSettings settings)
        {
            _mtSettings = mtSettings;
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings).SingleInstance();
            builder.RegisterInstance(_mtSettings.MtMarketMaker).SingleInstance();
            builder.RegisterInstance(_settings.RequestLoggerSettings).SingleInstance();
        }
    }
}
