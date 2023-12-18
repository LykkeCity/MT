// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    public class MonitoringSettings
    {
        [Optional]
        public QuotesMonitoringSettings Quotes { get; set; }   
    }
}