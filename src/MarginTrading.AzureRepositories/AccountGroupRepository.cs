using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.AzureRepositories.Contract;
using MarginTrading.Backend.Core.TradingConditions;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class AccountGroupEntity : TableEntity, IAccountGroup
    {
        public string TradingConditionId => PartitionKey;
        public string BaseAssetId => RowKey;

        decimal IAccountGroup.MarginCall => (decimal) MarginCall;
        public double MarginCall { get; set; }
        decimal IAccountGroup.StopOut => (decimal) StopOut;
        public double StopOut { get; set; }
        decimal IAccountGroup.DepositTransferLimit => (decimal) DepositTransferLimit;
        public double DepositTransferLimit { get; set; }
        decimal IAccountGroup.ProfitWithdrawalLimit => (decimal) ProfitWithdrawalLimit;
        public double ProfitWithdrawalLimit { get; set; }
        

        public static string GeneratePartitionKey(string tradingConditionId)
        {
            return tradingConditionId;
        }

        public static string GenerateRowKey(string baseAssetid)
        {
            return baseAssetid;
        }

        public static AccountGroupEntity Create(IAccountGroup src)
        {
            return new AccountGroupEntity
            {
                PartitionKey = GeneratePartitionKey(src.TradingConditionId),
                RowKey = GenerateRowKey(src.BaseAssetId),
                MarginCall = (double) src.MarginCall,
                StopOut = (double) src.StopOut,
                DepositTransferLimit = (double) src.DepositTransferLimit,
                ProfitWithdrawalLimit = (double) src.ProfitWithdrawalLimit
            };
        }
    }

    public class AccountGroupRepository : IAccountGroupRepository
    {
        private readonly INoSQLTableStorage<AccountGroupEntity> _tableStorage;

        public AccountGroupRepository(INoSQLTableStorage<AccountGroupEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddOrReplaceAsync(IAccountGroup group)
        {
            await _tableStorage.InsertOrReplaceAsync(AccountGroupEntity.Create(group));
        }

        public async Task<IAccountGroup> GetAsync(string tradingConditionId, string baseAssetId)
        {
            return await _tableStorage.GetDataAsync(AccountGroupEntity.GeneratePartitionKey(tradingConditionId),
                AccountGroupEntity.GenerateRowKey(baseAssetId));
        }

        public async Task<IEnumerable<IAccountGroup>> GetAllAsync()
        {
            return await _tableStorage.GetDataAsync();
        }
    }
}
