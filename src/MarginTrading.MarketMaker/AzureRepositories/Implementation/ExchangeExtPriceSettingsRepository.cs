using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.MarketMaker.AzureRepositories.Entities;
using MarginTrading.MarketMaker.Settings;

namespace MarginTrading.MarketMaker.AzureRepositories.Implementation
{
    internal class ExchangeExtPriceSettingsRepository : AbstractRepository<ExchangeExtPriceSettingsEntity>,
        IExchangeExtPriceSettingsRepository
    {
        public ExchangeExtPriceSettingsRepository(IReloadingManager<MarginTradingMarketMakerSettings> settings, ILog log)
            : base(AzureTableStorage<ExchangeExtPriceSettingsEntity>.Create(
                settings.Nested(s => s.Db.ConnectionString),
                "MarketMakerExchangeExtPriceSettings", log))
        {
        }

        public Task<IEnumerable<ExchangeExtPriceSettingsEntity>> GetAsync(string partitionKey)
        {
            return TableStorage.GetDataAsync(partitionKey);
        }

        public Task InsertOrReplaceAsync(IEnumerable<ExchangeExtPriceSettingsEntity> entities)
        {
            return TableStorage.InsertOrReplaceBatchAsync(entities);
        }
        
        public Task DeleteAsync(IEnumerable<ExchangeExtPriceSettingsEntity> entities)
        {
            return TableStorage.DeleteAsync(entities);
        }
    }
}