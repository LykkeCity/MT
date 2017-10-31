using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Backend.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingConditionEntity : TableEntity, IMarginTradingCondition
    {
        public string Id => RowKey;
        public string Name { get; set; }
        public bool IsDefault { get; set; }

        public static string GeneratePartitionKey()
        {
            return "TradingCondition";
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public static MarginTradingConditionEntity Create(IMarginTradingCondition src)
        {
            return new MarginTradingConditionEntity
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(src.Id),
                Name = src.Name,
                IsDefault = src.IsDefault
            };
        }
    }

    public class MarginTradingConditionsRepository : IMarginTradingConditionRepository
    {
        private readonly INoSQLTableStorage<MarginTradingConditionEntity> _tableStorage;

        public MarginTradingConditionsRepository(INoSQLTableStorage<MarginTradingConditionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddOrReplaceAsync(IMarginTradingCondition condition)
        {
            await _tableStorage.InsertOrReplaceAsync(MarginTradingConditionEntity.Create(condition));
        }

        public async Task<IMarginTradingCondition> GetAsync(string tradingConditionId)
        {
            return await _tableStorage.GetDataAsync(MarginTradingConditionEntity.GeneratePartitionKey(), MarginTradingConditionEntity.GenerateRowKey(tradingConditionId));
        }

        public async Task<IEnumerable<IMarginTradingCondition>> GetAllAsync()
        {
            return await _tableStorage.GetDataAsync();
        }
    }
}
