// Copyright (c) 2019 Lykke Corp.

namespace MarginTrading.Backend.Services.Assets
{
    public interface IAssetsCache
    {
        int GetAssetAccuracy(string assetId);
    }
}