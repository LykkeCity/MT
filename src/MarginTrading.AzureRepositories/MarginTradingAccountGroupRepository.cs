using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingAccountGroupEntity : TableEntity, IMarginTradingAccountGroup
    {
        public string TradingConditionId => PartitionKey;
        public string BaseAssetId => RowKey;

        decimal IMarginTradingAccountGroup.MarginCall => (decimal) MarginCall;
        public double MarginCall { get; set; }
        decimal IMarginTradingAccountGroup.StopOut => (decimal) StopOut;
        public double StopOut { get; set; }
        decimal IMarginTradingAccountGroup.DepositTransferLimit => (decimal) DepositTransferLimit;
        public double DepositTransferLimit { get; set; }

        public static string GeneratePartitionKey(string tradingConditionId)
        {
            return tradingConditionId;
        }

        public static string GenerateRowKey(string baseAssetid)
        {
            return baseAssetid;
        }

        public static MarginTradingAccountGroupEntity Create(IMarginTradingAccountGroup src)
        {
            return new MarginTradingAccountGroupEntity
            {
                PartitionKey = GeneratePartitionKey(src.TradingConditionId),
                RowKey = GenerateRowKey(src.BaseAssetId),
                MarginCall = (double) src.MarginCall,
                StopOut = (double) src.StopOut,
                DepositTransferLimit = (double) src.DepositTransferLimit
            };
        }
    }

    public class MarginTradingAccountGroupRepository : IMarginTradingAccountGroupRepository
    {
        private readonly INoSQLTableStorage<MarginTradingAccountGroupEntity> _tableStorage;

        public MarginTradingAccountGroupRepository(INoSQLTableStorage<MarginTradingAccountGroupEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddOrReplaceAsync(IMarginTradingAccountGroup group)
        {
            await _tableStorage.InsertOrReplaceAsync(MarginTradingAccountGroupEntity.Create(group));
        }

        public async Task<IMarginTradingAccountGroup> GetAsync(string tradingConditionId, string baseAssetId)
        {
            return await _tableStorage.GetDataAsync(MarginTradingAccountGroupEntity.GeneratePartitionKey(tradingConditionId),
                MarginTradingAccountGroupEntity.GenerateRowKey(baseAssetId));
        }

        public async Task<IEnumerable<IMarginTradingAccountGroup>> GetAllAsync()
        {
            return await _tableStorage.GetDataAsync();
        }
    }
}
