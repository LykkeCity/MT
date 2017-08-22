using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;

namespace MarginTrading.MarketMaker.Services
{
    public interface IAssetPairsSettingsService
    {
        [CanBeNull]
        AssetPairQuotesSourceEnum? GetAssetPairQuotesSource(string assetPairId);

        Task SetAssetPairQuotesSource(string assetPairId, AssetPairQuotesSourceEnum assetPairQuotesSource);

        Task<IReadOnlyDictionary<string, string>> GetAllPairsSources();
    }
}