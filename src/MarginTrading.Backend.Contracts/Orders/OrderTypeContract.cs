namespace MarginTrading.Backend.Contracts.Orders
{
    /// <summary>
    /// The type of order
    /// </summary>
    public enum OrderTypeContract
    {
        /// <summary>
        /// Market order, a basic one
        /// </summary>
        Market = 1,

        /// <summary>
        /// Limit order, a basic one
        /// </summary>
        Limit = 2,

        /// <summary>
        /// Stop order, a basic one (closing only)
        /// </summary>
        Stop = 3, //todo why this is not a related one?

        /// <summary>
        /// Take profit order, related to another parent one
        /// </summary>
        TakeProfit = 4,

        /// <summary>
        /// Stop loss order, related to another parent one
        /// </summary>
        StopLoss = 5, // todo what's the difference with Stop order

        /// <summary>
        /// Trailing stop order, related to another parent one
        /// </summary>
        TrailingStop = 6,
        
        //Closingout = 7,
        //Manual = 8
    }
}