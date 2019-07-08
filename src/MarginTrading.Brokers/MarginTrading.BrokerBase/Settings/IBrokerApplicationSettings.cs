// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.SettingsReader.Attributes;
using JetBrains.Annotations;

namespace MarginTrading.BrokerBase.Settings
{
    public interface IBrokerApplicationSettings<TBrokerSettings> 
        where TBrokerSettings : BrokerSettingsBase
    {
        SlackNotificationSettings SlackNotifications { get; }
        
        [Optional, CanBeNull]
        BrokersLogsSettings MtBrokersLogs { get; set; }
        
        BrokerSettingsRoot<TBrokerSettings> MtBackend { get; set; }
    }
}
