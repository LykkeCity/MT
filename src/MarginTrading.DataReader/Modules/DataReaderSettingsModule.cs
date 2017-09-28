using Autofac;
using MarginTrading.DataReader.Settings;

namespace MarginTrading.DataReader.Modules
{
    public class DataReaderSettingsModule : Module
    {
        private readonly DataReaderSettings _settings;

        public DataReaderSettingsModule(DataReaderSettings settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings).SingleInstance();
        }
    }
}
