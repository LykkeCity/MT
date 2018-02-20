using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.SettingsReader;
using MarginTrading.Backend.Services.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Backend.Services.Modules
{
    public class ExternalServicesModule : Module
    {
        private readonly IReloadingManager<MtBackendSettings> _settings;

        public ExternalServicesModule(IReloadingManager<MtBackendSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var services = new ServiceCollection();
            
            services.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.CurrentValue.Assets.ServiceUrl),
                _settings.CurrentValue.Assets.CacheExpirationPeriod));
            
            builder.Populate(services);

            builder.RegisterLykkeServiceClient(_settings.CurrentValue.ClientAccountServiceClient.ServiceUrl);
        }
    }
}