using MarginTrading.Backend.Core.DayOffSettings;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Common.Settings;

namespace MarginTrading.Backend.Services.Settings
{
    public class MtBackendSettings
    {
        public MarginTradingSettings MtBackend { get; set; }
        public EmailSenderSettings EmailSender { get; set; }
        public NotificationSettings Jobs { get; set; }
        public SlackNotificationSettings SlackNotifications { get; set; }
        public RiskInformingSettings RiskInformingSettings { get; set; }
        public RiskInformingSettings RiskInformingSettingsDemo { get; set; }
        public AssetClientSettings Assets { get; set; }
        public ClientAccountServiceSettings ClientAccountServiceClient { get; set; }
    }
}