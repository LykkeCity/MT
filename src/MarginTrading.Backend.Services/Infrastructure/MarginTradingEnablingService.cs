// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Autofac;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services.Notifications;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Common.Services.Client;
using MarginTrading.Common.Services.Settings;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public class MarginTradingEnablingService : IMarginTradingEnablingService, IStartable
    {
        private readonly IClientAccountService _clientAccountService;
        private readonly IMarginTradingSettingsCacheService _marginTradingSettingsCacheService;
        private readonly IRabbitMqProducerContainer _producerContainer;
        private readonly MarginTradingSettings _marginSettings;

        public MarginTradingEnablingService(IClientAccountService clientAccountService,
            MarginTradingSettings settings,
            IMarginTradingSettingsCacheService marginTradingSettingsCacheService,
            IRabbitMqProducerContainer producerContainer)
        {
            _marginSettings = settings;
            _clientAccountService = clientAccountService;
            _marginTradingSettingsCacheService = marginTradingSettingsCacheService;
            _producerContainer = producerContainer;
        }

        public async Task SetMarginTradingEnabled(string clientId, bool enabled)
        {
            var settings = await _clientAccountService.GetMarginEnabledAsync(clientId);

            settings.EnabledLive = enabled;
            settings.Enabled = enabled;

            await _clientAccountService.SetMarginEnabledAsync(clientId, settings.Enabled, settings.EnabledLive,
                settings.TermsOfUseAgreed);

            var marginEnabledChangedMessage = new MarginTradingEnabledChangedMessage
            {
                ClientId = clientId,
                EnabledDemo = settings.Enabled,
                EnabledLive = settings.EnabledLive
            };

            _marginTradingSettingsCacheService.OnMarginTradingEnabledChanged(marginEnabledChangedMessage);
            var (_, producer) = _producerContainer.GetProducer<MarginTradingEnabledChangedMessage>();
            await producer.ProduceAsync(marginEnabledChangedMessage);
        }

        public void Start()
        {
            _producerContainer.RegisterProducer<MarginTradingEnabledChangedMessage>(_marginSettings.RabbitMqQueues.MarginTradingEnabledChanged);
        }
    }
}