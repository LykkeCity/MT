// Copyright (c) 2019 Lykke Corp.

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