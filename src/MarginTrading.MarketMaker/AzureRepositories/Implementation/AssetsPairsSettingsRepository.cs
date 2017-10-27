using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Settings;

namespace MarginTrading.MarketMaker.AzureRepositories.Implementation
{
    internal class AssetsPairsSettingsRepository : AbstractRepository<AssetPairSettingsEntity>,
        IAssetsPairsSettingsRepository
    {
        public AssetsPairsSettingsRepository(IReloadingManager<MarginTradingMarketMakerSettings> settings, ILog log)
            : base(AzureTableStorage<AssetPairSettingsEntity>.Create(
                settings.Nested(s => s.Db.ConnectionString),
                "MarketMakerAssetPairsSettings", log))
        {
        }
    }
}