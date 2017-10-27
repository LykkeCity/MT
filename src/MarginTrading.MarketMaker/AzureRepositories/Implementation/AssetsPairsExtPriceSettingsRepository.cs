using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Settings;

namespace MarginTrading.MarketMaker.AzureRepositories.Implementation
{
    internal class AssetsPairsExtPriceSettingsRepository : AbstractRepository<AssetPairExtPriceSettingsEntity>,
        IAssetsPairsExtPriceSettingsRepository
    {
        public AssetsPairsExtPriceSettingsRepository(IReloadingManager<MarginTradingMarketMakerSettings> settings, ILog log)
            : base(AzureTableStorage<AssetPairExtPriceSettingsEntity>.Create(
                settings.Nested(s => s.Db.ConnectionString),
                "MarketMakerAssetPairsExtPriceSettings", log))
        {
        }
    }
}