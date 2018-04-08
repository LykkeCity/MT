using Autofac;
using Lykke.Service.ClientAccount.Client;
using Lykke.SettingsReader;
using MarginTrading.DataReader.Settings;

namespace MarginTrading.DataReader.Modules
{
    public class DataReaderExternalServicesModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public DataReaderExternalServicesModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterLykkeServiceClient(_settings.CurrentValue.ClientAccountServiceClient.ServiceUrl);
        }
    }
}