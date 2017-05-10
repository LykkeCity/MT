using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingWatchListEntity : TableEntity, IMarginTradingWatchList
    {
        public string Id { get; set; }
        public string AccountId { get; set; }
        public string Name { get; set; }
        public bool ReadOnly { get; set; }
        public int Order { get; set; }
        public string AssetIds { get; set; }

        List<string> IMarginTradingWatchList.AssetIds
            => AssetIds.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();

        public static string GeneratePartitionKey(string accountId)
        {
            return accountId;
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public static MarginTradingWatchListEntity Create(IMarginTradingWatchList src)
        {
            return new MarginTradingWatchListEntity
            {
                PartitionKey = GeneratePartitionKey(src.AccountId),
                RowKey = GenerateRowKey(src.Id),
                Id = src.Id,
                AccountId = src.AccountId,
                Name = src.Name,
                ReadOnly = src.ReadOnly,
                Order = src.Order,
                AssetIds = string.Join(",", src.AssetIds)
            };
        }
    }

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
                await _tableStorage.GetDataAsync(MarginTradingWatchListEntity.GeneratePartitionKey(watchList.AccountId),
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