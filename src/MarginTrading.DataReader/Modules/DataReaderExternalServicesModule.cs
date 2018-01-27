using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lykke.Service.Assets.Client;
using Lykke.SettingsReader;
using MarginTrading.DataReader.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.DataReader.Modules
{
    public class DataReaderExternalServicesModule : Module
    {
        private readonly IServiceCollection _services = new ServiceCollection();
        private readonly IReloadingManager<AppSettings> _settings;

        public DataReaderExternalServicesModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            _services.RegisterAssetsClient(AssetServiceSettings.Create(
                new Uri(_settings.CurrentValue.Assets.ServiceUrl),
                _settings.CurrentValue.Assets.CacheExpirationPeriod));
            
            builder.Populate(_services);
        }
    }
}