using System.ComponentModel;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    public class OvernightMarginSettings
    {
        /// <summary>
        /// Id of market which determines platform trading schedule.
        /// </summary>
        [Optional]
        public string ScheduleMarketId { get; set; } = "PlatformScheduleMarketId";

        /// <summary>
        /// Stop out warnings will be done this minutes before activating OvernightMarginParameter.
        /// </summary>
        [Optional]
        public int WarnPeriodMinutes { get; set; } = 30;

        /// <summary>
        /// OvernightMarginParameter will be activated this minutes before EOD.
        /// </summary>
        [Optional]
        public int ActivationPeriodMinutes { get; set; } = 30;
    }
}