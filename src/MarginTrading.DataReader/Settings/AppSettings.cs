using MarginTrading.Backend.Core.Settings;

namespace MarginTrading.DataReader.Settings
{
    public class AppSettings
    {
        public DataReaderLiveDemoSettings MtDataReader { get; set; }
        public SlackNotificationSettings SlackNotifications { get; set; }
        public AssetClientSettings Assets { get; set; }
    }
}
