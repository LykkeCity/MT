// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.BrokerBase.Settings;

namespace MarginTrading.ExternalOrderBroker.Settings
{
    public class AppSettings : BrokerSettingsBase
    {
        public DbSettings Db { get; set; }
        public RabbitMqQueuesSettings RabbitMqQueues { get; set; }
    }
}