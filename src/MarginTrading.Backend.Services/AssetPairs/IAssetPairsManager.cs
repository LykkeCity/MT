// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.Backend.Services.AssetPairs
{
    public interface IAssetPairsManager
    {
        /// <summary>
        /// Initialize asset pairs cache
        /// </summary>
        void InitAssetPairs();
    }
}