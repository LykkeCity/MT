// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using MarginTrading.Backend.Core.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Lykke.Snow.Mdm.Contracts.BrokerFeatures;

namespace MarginTrading.Backend.Extensions
{
    public static class FeatureManagementExtensions
    {
        /// <summary>
        /// Adds feature management with settings from MarginTradingSettings and remote features from Mdm
        /// </summary>
        /// <param name="services"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static void AddFeatureManagement(this IServiceCollection services,
            MarginTradingSettings settings)
        {
            var json = JsonConvert.SerializeObject(new { settings.FeatureManagement });

            var featuresConfigurationBuilder = new ConfigurationBuilder()
                .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)));

            services.AddFeatureManagement(settings.BrokerId, featuresConfigurationBuilder);
        }
    }
}