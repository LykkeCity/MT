using MarginTrading.Common.Settings;

namespace MarginTrading.DataReader.Settings
{
    public class AppSettings
    {
        public DataReaderLiveDemoSettings MtDataReader { get; set; }
        public SlackNotificationSettings SlackNotifications { get; set; }
        public ClientAccountServiceSettings ClientAccountServiceClient { get; set; }
    }
}
