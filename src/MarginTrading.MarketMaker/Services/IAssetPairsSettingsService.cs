using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;

namespace MarginTrading.MarketMaker.Services
{
    public interface IAssetPairsSettingsService
    {
        (AssetPairQuotesSourceTypeEnum? SourceType, string ExternalExchange) GetAssetPairQuotesSource(string assetPairId);

        Task SetAssetPairQuotesSource(string assetPairId, AssetPairQuotesSourceTypeEnum assetPairQuotesSourceType, [CanBeNull] string externalExchange);

        Task<IReadOnlyDictionary<string, string>> GetAllPairsSources();
    }
}