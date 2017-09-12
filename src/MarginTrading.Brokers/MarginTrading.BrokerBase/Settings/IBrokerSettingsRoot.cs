using Lykke.SettingsReader.Attributes;
using JetBrains.Annotations;

namespace MarginTrading.BrokerBase.Settings
{
    public interface IBrokerSettingsRoot
    {
        [Optional, CanBeNull]
        SlackNotificationSettings SlackNotifications { get; }
        [Optional, CanBeNull]
        MtBrokersLogsSettings MtBrokersLogs { get; set; }
    }
}
