namespace MarginTrading.Backend.Services.AssetPairs
{
    public interface IAssetPairDayOffService
    {
        /// <summary>
        /// Checks if now is day off for asset pair
        /// </summary>
        bool IsDayOff(string assetPairId);
        
        /// <summary>
        /// Checks if now creating new and executing existing pending orders with asset pair is disabled
        /// </summary>
        bool ArePendingOrdersDisabled(string assetPairId);
    }
}
