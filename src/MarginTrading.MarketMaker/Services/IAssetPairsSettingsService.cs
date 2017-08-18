using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;

namespace MarginTrading.MarketMaker.Services
{
    internal interface IAssetPairsSettingsService
    {
        [CanBeNull]
        AssetPairQuotesSourceEnum? GetAssetPairQuotesSource(string assetPairId);

        Task SetAssetPairQuotesSource(string assetPairId, AssetPairQuotesSourceEnum assetPairQuotesSource);
    }
}