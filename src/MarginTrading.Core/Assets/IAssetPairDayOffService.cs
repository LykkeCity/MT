namespace MarginTrading.Core.Assets
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
        bool IsPendingOrderDisabled(string assetPairId);
        
        /// <summary>
        /// Checks if now creating new and executing existing pending orders with any of asset pairs is disabled
        /// </summary>
        bool IsPendingOrdersDisabledTime();
        
        /// <summary>
        /// Checks if asset pair is trading 24/7
        /// </summary>
        bool IsAssetPairHasNoDayOff(string assetPairId);
    }
}
