using System.Threading.Tasks;
using Autofac;
using Common;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Common.RabbitMq;
using MarginTrading.Common.Services.Client;
using MarginTrading.Common.Services.Settings;

namespace MarginTrading.Backend.Services
{
    public class MarginTradingEnablingService : IMarginTradingEnablingService, IStartable
    {
        private IMessageProducer<MarginTradingEnabledChangedMessage> _eventsPublisher;
        private readonly IClientAccountService _clientAccountService;
        private readonly IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;
        private readonly IRabbitMqService _rabbitMqService;
        private readonly MarginTradingSettings _marginSettings;

        public MarginTradingEnablingService(IClientAccountService clientAccountService, IRabbitMqService rabbitMqService,
            MarginTradingSettings settings,
            IMarginTradingSettingsCacheService marginTradingSettingsCacheService)
        {
            _marginSettings = settings;
            _rabbitMqService = rabbitMqService;
            _clientAccountService = clientAccountService;
            _marginTradingSettingsCacheService = marginTradingSettingsCacheService;
        }

        public async Task SetMarginTradingEnabled(string clientId, bool enabled)
        {
            var settings = await _clientAccountService.GetMarginEnabledAsync(clientId);

            if (_marginSettings.IsLive)
            {
                settings.EnabledLive = enabled;
            }
            else
            {
                settings.Enabled = enabled;
            }

            await _clientAccountService.SetMarginEnabledAsync(clientId, settings.Enabled, settings.EnabledLive,
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
            _eventsPublisher = _rabbitMqService.GetProducer(
                new RabbitMqSettings
                {
                    ConnectionString = _marginSettings.MtRabbitMqConnString,
                    ExchangeName = _marginSettings.RabbitMqQueues.MarginTradingEnabledChanged.ExchangeName
                },
                true,
                _rabbitMqService.GetJsonSerializer<MarginTradingEnabledChangedMessage>());
        }
    }
}