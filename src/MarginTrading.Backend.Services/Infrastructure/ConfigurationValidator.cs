// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.Backend.Core.Settings;
using Microsoft.Extensions.Logging;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public sealed class ConfigurationValidator : IConfigurationValidator
    {
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly ILogger<ConfigurationValidator> _logger;

        public ConfigurationValidator(MarginTradingSettings marginTradingSettings, ILogger<ConfigurationValidator> logger)
        {
            _marginTradingSettings = marginTradingSettings;
            _logger = logger;
        }

        public void WarnOrThrowIfInvalid()
        {
            if (_marginTradingSettings.CompiledSchedulePublishing.IsEnabled)
            {
                _logger.LogWarning("Compiled schedule publishing feature is obsolete but it is enabled.");
            }

            if (_marginTradingSettings.TradeContractPublishing.IsEnabled)
            {
                _logger.LogWarning("Trade contract publishing feature is obsolete but it is enabled.");
            }
        }
    }
}