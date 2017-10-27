using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MarginTrading.MarketMaker.AzureRepositories.Entities;

namespace MarginTrading.MarketMaker.AzureRepositories
{
    internal interface IExchangeExtPriceSettingsRepository : IAbstractRepository<ExchangeExtPriceSettingsEntity>
    {
        Task<IEnumerable<ExchangeExtPriceSettingsEntity>> GetAsync(string partitionKey);
        Task InsertOrReplaceAsync(IEnumerable<ExchangeExtPriceSettingsEntity> entities);
        Task DeleteAsync(IEnumerable<ExchangeExtPriceSettingsEntity> entities);
    }
}