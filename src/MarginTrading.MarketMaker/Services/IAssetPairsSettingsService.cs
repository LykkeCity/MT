using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;
using MarginTrading.MarketMaker.Models.Api;

namespace MarginTrading.MarketMaker.Services
{
    public interface IAssetPairsSettingsService
    {
        [CanBeNull]
        AssetPairQuotesSourceTypeEnum? GetAssetPairQuotesSource(string assetPairId);

        Task SetAssetPairQuotesSourceAsync(string assetPairId, AssetPairQuotesSourceTypeEnum assetPairQuotesSourceType);

        Task<List<AssetPairSettings>> GetAllPairsSourcesAsync();

        Task DeleteAsync(string assetPairId);

        AssetPairSettings Get(string assetPairId);
    }
}