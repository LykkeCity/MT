using System.ComponentModel;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.Backend.Core.Settings
{
    public class OvernightMarginSettings
    {
        /// <summary>
        /// Name of market which determines platform trading schedule.
        /// </summary>
        [Optional]
        public string ScheduleMarketId { get; set; } = "PlatformScheduleMarketId";

        /// <summary>
        /// Multiplier for the intraday margin parameter to be active at night. 
        /// </summary>
        [Optional]
        public decimal OvernightMarginParameter { get; set; } = 3;

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