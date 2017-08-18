namespace MarginTrading.MarketMaker.Enums
{
    /// <summary>
    /// The quotes source for the asset pair
    /// </summary>
    public enum AssetPairQuotesSourceEnum
    {
        /// <summary>
        /// Quotes are provided only manually - used for test purposes
        /// </summary>
        Manual = 1,

        /// <summary>
        /// Quotes are provided only from ICM
        /// </summary>
        Icm = 2,

        /// <summary>
        /// Quotes are provided from the spot orderbook
        /// </summary>
        Spot = 3,
    }
}