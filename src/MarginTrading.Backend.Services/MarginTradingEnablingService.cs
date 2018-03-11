using System.Threading.Tasks;
using Autofac;
using Common;
using Lykke.Service.ClientAccount.Client;
using MarginTrading.Backend.Contracts.RabbitMqMessages;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services.Settings;

namespace MarginTrading.Backend.Services
{
    public class MarginTradingEnablingService : IMarginTradingEnablingService, IStartable
    {
        private IMessageProducer<MarginTradingEnabledChangedMessage> _eventsPublisher;
        private readonly IClientAccountClient _clientAccountClient;
        private readonly IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly MarginSettings _marginSettings;

        public MarginTradingEnablingService(IClientAccountClient clientAccountClient, IRabbitMqService rabbitMqService,
            MarginSettings settings,
            IMarginTradingSettingsCacheService marginTradingSettingsCacheService)
        {
            _marginSettings = settings;
            _rabbitMqService = rabbitMqService;
            _clientAccountClient = clientAccountClient;
            _marginTradingSettingsCacheService = marginTradingSettingsCacheService;
        }

        public async Task SetMarginTradingEnabled(string clientId, bool enabled)
        {
            var settings = await _clientAccountClient.GetMarginEnabledAsync(clientId);

            if (_marginSettings.IsLive)
            {
                settings.EnabledLive = enabled;
            }
            else
            {
                settings.Enabled = enabled;
            }

            await _clientAccountClient.SetMarginEnabledAsync(clientId, settings.Enabled, settings.EnabledLive,
                settings.TermsOfUseAgreed);

            var marginEnabledChangedMessage = new MarginTradingEnabledChangedMessage
            {
                ClientId = clientId,
                EnabledDemo = settings.Enabled,
                EnabledLive = settings.EnabledLive
            };

            _marginTradingSettingsCacheService.OnMarginTradingEnabledChanged(marginEnabledChangedMessage);
            await _eventsPublisher.ProduceAsync(marginEnabledChangedMessage);
        }

        public void Start()
        {
            _eventsPublisher = _rabbitMqService.GetProducer(new RabbitMqSettings
            {
                ConnectionString = _marginSettings.MtRabbitMqConnString,
                ExchangeName = _marginSettings.RabbitMqQueues.MarginTradingEnabledChanged.ExchangeName,
            }, false, _rabbitMqService.GetJsonSerializer<MarginTradingEnabledChangedMessage>());
        }
    }
}