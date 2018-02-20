using Autofac;
using Lykke.Service.ClientAccount.Client;
using Lykke.SettingsReader;
using MarginTrading.Frontend.Settings;

namespace MarginTrading.Frontend.Modules
{
    public class FrontendExternalServicesModule: Module
    {
        private readonly IReloadingManager<ApplicationSettings> _settings;

        public FrontendExternalServicesModule(IReloadingManager<ApplicationSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            
            builder.RegisterLykkeServiceClient(_settings.CurrentValue.ClientAccountServiceClient.ServiceUrl);
        }
    }
}