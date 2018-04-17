using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.HttpClientGenerator;
using Lykke.HttpClientGenerator.Retries;
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
                HttpClientGenerator.BuildForUrl(_settings.CurrentValue.MtDataReaderDemoServiceClient.ServiceUrl)
                    .WithApiKey(_settings.CurrentValue.MtDataReaderDemoServiceClient.ApiKey)
                    .Create(), 
                HttpClientGenerator.BuildForUrl(_settings.CurrentValue.MtDataReaderLiveServiceClient.ServiceUrl)
                    .WithApiKey(_settings.CurrentValue.MtDataReaderLiveServiceClient.ApiKey)
                    .Create());
            
            builder.RegisterLykkeServiceClient(_settings.CurrentValue.ClientAccountServiceClient.ServiceUrl);
            
            builder.Populate(services);
        }
    }
}