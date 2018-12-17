namespace MarginTrading.Backend.Core.Orders
{
    public enum FxToAssetPairDirection
    {
        /// <summary>
        /// AssetPair is {BaseId, QuoteId} and FxAssetPair is {QuoteId, AccountAssetId}
        /// </summary>
        Straight,
        
        /// <summary>
        /// AssetPair is {BaseId, QuoteId} and FxAssetPair is {AccountAssetId, QuoteId}
        /// </summary>
        Reverse,
    }
}