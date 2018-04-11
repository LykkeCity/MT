using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.HttpClientGenerator;
using Lykke.Service.ClientAccount.Client;
using Lykke.SettingsReader;
using MarginTrading.Backend.Contracts.DataReaderClient;
using MarginTrading.Frontend.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Frontend.Modules
{
    public class FrontendExternalServicesModule : Module
    {
        private readonly IReloadingManager<ApplicationSettings> _settings;

        public FrontendExternalServicesModule(IReloadingManager<ApplicationSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var services = new ServiceCollection();
            services.RegisterMtDataReaderClientsPair(
                HttpClientGenerator.CreateDefault(
                    _settings.CurrentValue.MtDataReaderDemoServiceClient.ServiceUrl,
                    _settings.CurrentValue.MtDataReaderDemoServiceClient.ApiKey), 
                HttpClientGenerator.CreateDefault(
                    _settings.CurrentValue.MtDataReaderLiveServiceClient.ServiceUrl,
                    _settings.CurrentValue.MtDataReaderLiveServiceClient.ApiKey));
            
            builder.RegisterLykkeServiceClient(_settings.CurrentValue.ClientAccountServiceClient.ServiceUrl);
            
            builder.Populate(services);
        }
    }
}