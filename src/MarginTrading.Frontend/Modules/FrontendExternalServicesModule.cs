using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.HttpClientGenerator;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.PersonalData.Contract;
using Lykke.SettingsReader;
using MarginTrading.Backend.Contracts.DataReaderClient;
using MarginTrading.Frontend.Settings;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace MarginTrading.Frontend.Modules
{
    public class FrontendExternalServicesModule : Module
    {
        private readonly IReloadingManager<ApplicationSettings> _settings;
        private readonly ILog _log;

        public FrontendExternalServicesModule(IReloadingManager<ApplicationSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
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
            
            var personalDataServiceMock = new Mock<IPersonalDataService>(MockBehavior.Strict);
            builder.RegisterInstance(personalDataServiceMock.Object).As<IPersonalDataService>();
            
            builder.Populate(services);
        }
    }
}
