namespace MarginTrading.BrokerBase.Settings
{
    public class DefaultBrokerSettings: IBrokerSettingsRoot
    {
        public SlackNotificationSettings SlackNotifications { get; set; }
        public MtBrokersLogsSettings MtBrokersLogs { get; set; }
    }

    public class MtBrokersLogsSettings
    {
        public string DbConnString { get; set; }
    }
}
