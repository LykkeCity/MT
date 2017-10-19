using MarginTrading.Core.Settings;

namespace MarginTrading.Services.Settings
{
    public class MtBackendSettings
    {
        public MarginTradingSettings MtBackend { get; set; }
        public EmailSenderSettings EmailSender { get; set; }
        public NotificationSettings Jobs { get; set; }
        public ScheduleSettings MtSchedule { get; set; }
        public SlackNotificationSettings SlackNotifications { get; set; }
    }
}