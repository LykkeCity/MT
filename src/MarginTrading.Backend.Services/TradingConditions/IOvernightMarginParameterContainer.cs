namespace MarginTrading.Backend.Services.TradingConditions
{
    /// <summary>
    /// Container for the current margin parameter state.
    /// </summary>
    public interface IOvernightMarginParameterContainer
    {
        /// <summary>
        /// Multiplier for the intraday margin parameter to be active at night. 
        /// </summary>
        decimal OvernightMarginParameter { get; set; }
    }
}