using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core.TradingConditions;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class TradingConditionEntity : TableEntity, ITradingCondition
    {
        public string Id => RowKey;
        public string Name { get; set; }
        public string MatchingEngineId { get; set; }
        public bool IsDefault { get; set; }

        public static string GeneratePartitionKey()
        {
            return "TradingCondition";
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public static TradingConditionEntity Create(ITradingCondition src)
        {
            return new TradingConditionEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(src.Id),
                Name = src.Name,
                MatchingEngineId = src.MatchingEngineId,
                IsDefault = src.IsDefault
            };
        }
    }

    public class TradingConditionsRepository : ITradingConditionRepository
    {
        private readonly INoSQLTableStorage<TradingConditionEntity> _tableStorage;

        public TradingConditionsRepository(INoSQLTableStorage<TradingConditionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddOrReplaceAsync(ITradingCondition condition)
        {
            await _tableStorage.InsertOrReplaceAsync(TradingConditionEntity.Create(condition));
        }

        public async Task<ITradingCondition> GetAsync(string tradingConditionId)
        {
            return await _tableStorage.GetDataAsync(TradingConditionEntity.GeneratePartitionKey(), TradingConditionEntity.GenerateRowKey(tradingConditionId));
        }

        public async Task<IEnumerable<ITradingCondition>> GetAllAsync()
        {
            return await _tableStorage.GetDataAsync();
        }
    }
}
