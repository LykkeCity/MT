using AzureStorage;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingPositionRepository : IMarginTradingPositionRepository
    {
        private readonly INoSQLTableStorage<MarginTradingPositionEntity> _tableStorage;

        public MarginTradingPositionRepository(INoSQLTableStorage<MarginTradingPositionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddAsync(IPosition position)
        {
            await _tableStorage.InsertAsync(MarginTradingPositionEntity.Create(position));
        }

        public async Task<IEnumerable<IPosition>> GetAllAsync()
        {
            return await _tableStorage.GetDataAsync();
        }

        public async Task<IPosition> GetAsync(string clientId, string asset)
        {
            return await _tableStorage.GetDataAsync(
                MarginTradingPositionEntity.GeneratePartitionKey(asset),
                MarginTradingPositionEntity.GenerateRowKey(clientId, asset));
        }

        public async Task<IEnumerable<IPosition>> GetByAssetAsync(string asset)
        {
            return await _tableStorage.GetDataAsync(
                MarginTradingPositionEntity.GeneratePartitionKey(asset));
        }

        public async Task<IEnumerable<IPosition>> GetByClentAsync(string clientId, string[] assets)
        {
            return await _tableStorage.GetDataAsync(assets.Select(x => new Tuple<string, string>(
                MarginTradingPositionEntity.GeneratePartitionKey(x),
                MarginTradingPositionEntity.GenerateRowKey(clientId, x))));
        }

        public async Task UpdateAsync(IPosition position)
        {
            await _tableStorage.InsertOrMergeAsync(MarginTradingPositionEntity.Create(position));
        }
    }
}
