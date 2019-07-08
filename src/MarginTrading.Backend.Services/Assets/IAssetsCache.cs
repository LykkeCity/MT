// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Services.Assets
{
    public interface IAssetsCache
    {
        int GetAssetAccuracy(string assetId);
    }
}