using Autofac;
using Autofac.Extensions.DependencyInjection;
using Common.Log;
using Lykke.Service.ClientAccount.Client;
using Lykke.Service.EmailSender;
using Lykke.Service.ExchangeConnector.Client;
using Lykke.SettingsReader;
using MarginTrading.Backend.Services.Settings;
using MarginTrading.Backend.Services.Stubs;
using MarginTrading.Common.Services.Client;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.Backend.Modules
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
            
            builder.RegisterType<ExchangeConnectorService>()
                .As<IExchangeConnectorService>()
                .WithParameter("settings", _settings.CurrentValue.MtStpExchangeConnectorClient)
                .SingleInstance();
            
            builder.Populate(services);

            if (_settings.CurrentValue.ClientAccountServiceClient != null)
            {
                builder.RegisterLykkeServiceClient(_settings.CurrentValue.ClientAccountServiceClient.ServiceUrl);
                
                builder.RegisterType<ClientAccountService>()
                    .As<IClientAccountService>()
                    .SingleInstance();
            }
            else
            {
                builder.RegisterType<ClientAccountServiceEmptyStub>()
                    .As<IClientAccountService>()
                    .SingleInstance();
            }

            if (_settings.CurrentValue.EmailSender != null)
            {
                builder.Register<IEmailSender>(ctx =>
                    new EmailSenderClient(_settings.CurrentValue.EmailSender.ServiceUrl, ctx.Resolve<ILog>())
                ).SingleInstance();
            }
            else
            {
                builder.RegisterType<EmailSenderLogStub>().As<IEmailSender>();
            }
        }
    }
}