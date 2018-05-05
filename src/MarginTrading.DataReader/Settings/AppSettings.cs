using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.DataReader.Settings
{
    public class AppSettings
    {
        public DataReaderSettings MtDataReader { get; set; }
        [Optional, CanBeNull] public SlackNotificationSettings SlackNotifications { get; set; }
    }
}