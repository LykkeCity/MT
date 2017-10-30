namespace MarginTrading.Common.Settings.Models
{
    public class PushNotificationsSettings : TraderSettingsBase
    {
        public override string GetId()
        {
            return "PushNotificationsSettings";
        }

        public bool Enabled { get; set; }

        public static PushNotificationsSettings CreateDefault()
        {
            return new PushNotificationsSettings
            {
                Enabled = true
            };
        }
    }
}