using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IAssetPairsSettingsService
    {
        (AssetPairQuotesSourceTypeEnum? SourceType, string ExternalExchange) GetAssetPairQuotesSource(string assetPairId);

        Task SetAssetPairQuotesSourceAsync(string assetPairId, AssetPairQuotesSourceTypeEnum assetPairQuotesSourceType, [CanBeNull] string externalExchange);

        Task<List<AssetPairSettings>> GetAllPairsSourcesAsync();

        Task DeleteAsync(string assetPairId);

        AssetPairSettings Get(string assetPairId);
    }
}