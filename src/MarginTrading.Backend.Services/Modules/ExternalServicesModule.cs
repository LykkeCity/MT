using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.ExchangeConnector.Client;
using Lykke.Service.PersonalData.Client;
using Lykke.Service.PersonalData.Contract;
using Lykke.SettingsReader;
using MarginTrading.Backend.Services.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Backend.Services.Modules
{
    public class ExternalServicesModule : Module
    {
        private readonly IReloadingManager<MtBackendSettings> _settings;
        private readonly ILog _log;

        public ExternalServicesModule(IReloadingManager<MtBackendSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var services = new ServiceCollection();
            
            services.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.CurrentValue.Assets.ServiceUrl),
                _settings.CurrentValue.Assets.CacheExpirationPeriod));

            builder.RegisterType<ExchangeConnectorService>()
                .As<IExchangeConnectorService>()
                .WithParameter("settings", _settings.CurrentValue.MtStpExchangeConnectorClient)
                .SingleInstance();
            
            builder.Populate(services);

            builder.RegisterLykkeServiceClient(_settings.CurrentValue.ClientAccountServiceClient.ServiceUrl);

            builder.RegisterInstance<IPersonalDataService>(
                new PersonalDataService(_settings.CurrentValue.PersonalDataServiceSettings, _log))
            .SingleInstance();
        }
    }
}
