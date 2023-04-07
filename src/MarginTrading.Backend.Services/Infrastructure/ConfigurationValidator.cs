// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace MarginTrading.Backend.Services.Infrastructure
{
    public sealed class ConfigurationValidator : IConfigurationValidator
    {
        private readonly ILogger<ConfigurationValidator> _logger;
        private readonly IFeatureManager _featureManager;

        public ConfigurationValidator(ILogger<ConfigurationValidator> logger, IFeatureManager featureManager)
        {
            _logger = logger;
            _featureManager = featureManager;
        }

        public async Task WarnIfInvalidAsync()
        {
            if (await _featureManager.IsEnabledAsync(Feature.CompiledSchedulePublishing.ToString("G")))
            {
                _logger.LogWarning("Compiled schedule publishing feature is obsolete but it is enabled.");
            }

            if (await _featureManager.IsEnabledAsync(Feature.TradeContractPublishing.ToString("G")))
            {
                _logger.LogWarning("Trade contract publishing feature is obsolete but it is enabled.");
            }
        }
    }
}