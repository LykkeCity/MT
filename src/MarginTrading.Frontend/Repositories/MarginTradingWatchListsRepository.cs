using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Frontend.Repositories.Contract;
using MarginTrading.Frontend.Repositories.Entities;

namespace MarginTrading.Frontend.Repositories
{
    public class MarginTradingWatchListsRepository : IMarginTradingWatchListRepository
    {
        private readonly INoSQLTableStorage<MarginTradingWatchListEntity> _tableStorage;

        public MarginTradingWatchListsRepository(INoSQLTableStorage<MarginTradingWatchListEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IMarginTradingWatchList> AddAsync(IMarginTradingWatchList watchList)
        {
            await _tableStorage.InsertOrReplaceAsync(MarginTradingWatchListEntity.Create(watchList));
            var entity =
                await _tableStorage.GetDataAsync(MarginTradingWatchListEntity.GeneratePartitionKey(watchList.ClientId),
                    MarginTradingWatchListEntity.GenerateRowKey(watchList.Id));

            return MarginTradingWatchList.Create(entity);
        }

        public async Task<IEnumerable<IMarginTradingWatchList>> GetAllAsync(string accountId)
        {
            var entities = await _tableStorage.GetDataAsync(MarginTradingWatchListEntity.GeneratePartitionKey(accountId));

            return entities.Select(MarginTradingWatchList.Create).OrderBy(item => item.Order);
        }

        public async Task DeleteAsync(string accountId, string id)
        {
            await _tableStorage.DeleteAsync(MarginTradingWatchListEntity.GeneratePartitionKey(accountId),
                MarginTradingWatchListEntity.GenerateRowKey(id));
        }

        public async Task<IMarginTradingWatchList> GetAsync(string accountId, string id)
        {
            var entity = await _tableStorage.GetDataAsync(MarginTradingWatchListEntity.GeneratePartitionKey(accountId),
                MarginTradingWatchListEntity.GenerateRowKey(id));

            return entity == null
                ? null
                : MarginTradingWatchListEntity.Create(entity);
        }

        public async Task ChangeAllAsync(IEnumerable<IMarginTradingWatchList> watchLists)
        {
            await _tableStorage.InsertOrReplaceAsync(watchLists.Select(MarginTradingWatchListEntity.Create));
        }
    }
}