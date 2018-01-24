namespace MarginTrading.Backend.Services.Assets
{
    public interface IAssetsCache
    {
        int GetAssetAccuracy(string assetId);
    }
}