namespace MarginTrading.Common.Services.Settings
{
    /// <summary>
    /// Contains information about what types of margin trading are available for particular client
    /// </summary>
    public class EnabledMarginTradingTypes
    {
        /// <summary>
        /// Is demo margin trading enabled for client
        /// </summary>
        public bool Demo { get; set; }

        /// <summary>
        /// Is live margin trading enabled for client
        /// </summary>
        public bool Live { get; set; }
    }
}
