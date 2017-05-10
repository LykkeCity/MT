using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingAccountEntity : TableEntity, IMarginTradingAccount
    {
        public string Id => RowKey;
        public string ClientId => PartitionKey;
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public double Balance { get; set; }
        public bool IsCurrent { get; set; }
        public double MarginCall { get; set; }
        public double StopOut { get; set; }

        public static string GeneratePartitionKey(string clientId)
        {
            return clientId;
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public static MarginTradingAccountEntity Create(IMarginTradingAccount src)
        {
            return new MarginTradingAccountEntity
            {
                PartitionKey = GeneratePartitionKey(src.ClientId),
                RowKey = GenerateRowKey(src.Id),
                TradingConditionId = src.TradingConditionId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                IsCurrent = src.IsCurrent
            };
        }
    }

    public class MarginTradingAccountsRepository : IMarginTradingAccountsRepository
    {
        private readonly INoSQLTableStorage<MarginTradingAccountEntity> _tableStorage;

        public MarginTradingAccountsRepository(INoSQLTableStorage<MarginTradingAccountEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IEnumerable<IMarginTradingAccount>> GetAllAsync(string clientId = null)
        {
            var entities = string.IsNullOrEmpty(clientId)
                ? await _tableStorage.GetDataAsync()
                : await _tableStorage.GetDataAsync(MarginTradingAccountEntity.GeneratePartitionKey(clientId));

            return entities.Select(MarginTradingAccount.Create);
        }

        public async Task<MarginTradingAccount> UpdateBalanceAsync(string clientId, string accountId, double amount)
        {
            var account = await _tableStorage.GetDataAsync(MarginTradingAccountEntity.GeneratePartitionKey(clientId), MarginTradingAccountEntity.GenerateRowKey(accountId));

            if (account != null)
            {
                account.Balance += amount;
                await _tableStorage.InsertOrMergeAsync(account);
                return MarginTradingAccount.Create(account);
            }

            return null;
        }

        public async Task<bool> UpdateTradingConditionIdAsync(string accountId, string tradingConditionId)
        {
            var account = (await _tableStorage.GetDataAsync(entity => entity.Id == accountId)).FirstOrDefault();

            if (account != null)
            {
                account.TradingConditionId = tradingConditionId;
                await _tableStorage.InsertOrMergeAsync(account);
                return true;
            }

            return false;
        }

        public async Task AddAsync(MarginTradingAccount account)
        {
            var accounts = await GetAllAsync(account.ClientId);

            if (!accounts.Any())
            {
                account.IsCurrent = true;
            }

            var entity = MarginTradingAccountEntity.Create(account);
            await _tableStorage.InsertOrMergeAsync(entity);
        }

        public async Task<IMarginTradingAccount> GetAsync(string clientId, string accountId)
        {
            var account = await _tableStorage.GetDataAsync(MarginTradingAccountEntity.GeneratePartitionKey(clientId), MarginTradingAccountEntity.GenerateRowKey(accountId));

            return account != null
                ? MarginTradingAccount.Create(account)
                : null;
        }

        public async Task<IMarginTradingAccount> GetAsync(string accountId)
        {
            var entities = await _tableStorage.GetDataAsync(entity => entity.Id == accountId);
            var account =  entities.FirstOrDefault();

            return account != null
                ? MarginTradingAccount.Create(account)
                : null;
        }

        public async Task<IMarginTradingAccount> SetActiveAsync(string clientId, string accountId)
        {
            var accounts = await _tableStorage.GetDataAsync(MarginTradingAccountEntity.GeneratePartitionKey(clientId));

            IMarginTradingAccount currentAccount = null;

            foreach (var account in accounts)
            {
                account.IsCurrent = account.Id == accountId;

                if (account.IsCurrent)
                {
                    currentAccount = account;
                }

                await _tableStorage.InsertOrMergeAsync(account);
            }

            return currentAccount != null 
                ? MarginTradingAccount.Create(currentAccount) 
                : null;
        }

        public async Task DeleteAndSetActiveIfNeededAsync(string clientId, string accountId)
        {
            var accounts = (await GetAllAsync(clientId)).ToArray();

            var accountToDelete = accounts.FirstOrDefault(item => item.ClientId == clientId && item.Id == accountId);

            if (accountToDelete == null)
                return;

            await _tableStorage.DeleteAsync(MarginTradingAccountEntity.GeneratePartitionKey(clientId),
                MarginTradingAccountEntity.GenerateRowKey(accountId));

            if (accountToDelete.IsCurrent)
            {
                var newCurrentAccount = accounts.FirstOrDefault(item => item.Id != accountToDelete.Id);

                if (newCurrentAccount != null)
                {
                    await SetActiveAsync(clientId, newCurrentAccount.Id);
                }
            }

        }
    }
}
