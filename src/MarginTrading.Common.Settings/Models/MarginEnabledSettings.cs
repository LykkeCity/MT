namespace MarginTrading.Common.Settings.Models
{
    public class MarginEnabledSettings : TraderSettingsBase
    {
        public override string GetId()
        {
            return "MarginEnabledSettings";
        }

        public bool Enabled { get; set; }
        public bool EnabledLive { get; set; }

        public static MarginEnabledSettings CreateDefault()
        {
            return new MarginEnabledSettings
            {
                Enabled = true,
                EnabledLive = false
            };
        }
    }
}