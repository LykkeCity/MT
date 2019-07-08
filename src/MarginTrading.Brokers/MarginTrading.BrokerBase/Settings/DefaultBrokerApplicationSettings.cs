// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.BrokerBase.Settings
{
    public class DefaultBrokerApplicationSettings<TBrokerSettings>: IBrokerApplicationSettings<TBrokerSettings>
        where TBrokerSettings: BrokerSettingsBase
    {
        public SlackNotificationSettings SlackNotifications { get; set; }
        public BrokersLogsSettings MtBrokersLogs { get; set; }
        public BrokerSettingsRoot<TBrokerSettings> MtBackend { get; set; }
    }
}
