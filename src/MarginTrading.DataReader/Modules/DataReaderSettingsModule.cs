using Autofac;
using Lykke.SettingsReader;
using MarginTrading.DataReader.Settings;

namespace MarginTrading.DataReader.Modules
{
    public class DataReaderSettingsModule : Module
    {
        private readonly IReloadingManager<DataReaderSettings> _settings;

        public DataReaderSettingsModule(IReloadingManager<DataReaderSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings).SingleInstance();
            builder.RegisterInstance(_settings.CurrentValue).SingleInstance();
        }
    }
}
