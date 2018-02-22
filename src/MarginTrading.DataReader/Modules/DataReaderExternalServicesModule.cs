using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.Service.Assets.Client;
using Lykke.Service.ClientAccount.Client;
using Lykke.SettingsReader;
using MarginTrading.DataReader.Settings;
using Microsoft.Extensions.DependencyInjection;

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
            var services = new ServiceCollection();
            
            services.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.CurrentValue.Assets.ServiceUrl),
                _settings.CurrentValue.Assets.CacheExpirationPeriod));
            
            builder.Populate(services);
            
            builder.RegisterLykkeServiceClient(_settings.CurrentValue.ClientAccountServiceClient.ServiceUrl);
        }
    }
}