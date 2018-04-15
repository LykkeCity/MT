using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using MarginTrading.Backend.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingAccountEntity : TableEntity, IMarginTradingAccount
    {
        public string Id => RowKey;
        public string ClientId => PartitionKey;
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        decimal IMarginTradingAccount.Balance => (decimal) Balance;
        public double Balance { get; set; }
        decimal IMarginTradingAccount.WithdrawTransferLimit => (decimal) WithdrawTransferLimit;
        public AccountFpl AccountFpl => new AccountFpl();
        public string LegalEntity { get; set; }
        public double WithdrawTransferLimit { get; set; }
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
                Balance = (double) src.Balance,
                WithdrawTransferLimit = (double) src.WithdrawTransferLimit,
                LegalEntity = src.LegalEntity,
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
            return string.IsNullOrEmpty(clientId)
                ? await _tableStorage.GetDataAsync()
                : await _tableStorage.GetDataAsync(MarginTradingAccountEntity.GeneratePartitionKey(clientId));
        }

        public async Task<MarginTradingAccount> UpdateBalanceAsync(string clientId, string accountId, decimal amount, bool changeLimit)
        {
            var account = await _tableStorage.GetDataAsync(MarginTradingAccountEntity.GeneratePartitionKey(clientId), MarginTradingAccountEntity.GenerateRowKey(accountId));

            if (account != null)
            {
                account.Balance += (double) amount;

                if (changeLimit)
                    account.WithdrawTransferLimit += (double) amount;

                await _tableStorage.InsertOrMergeAsync(account);
                return MarginTradingAccount.Create(account);
            }

            return null;
        }

        public async Task<IMarginTradingAccount> UpdateTradingConditionIdAsync(string clientId, string accountId,
            string tradingConditionId)
        {
            return await _tableStorage.MergeAsync(MarginTradingAccountEntity.GeneratePartitionKey(clientId),
                MarginTradingAccountEntity.GenerateRowKey(accountId),
                a =>
                {
                    a.TradingConditionId = tradingConditionId;
                    return a;
                });
        }

        public async Task AddAsync(MarginTradingAccount account)
        {
            var entity = MarginTradingAccountEntity.Create(account);
            await _tableStorage.InsertOrMergeAsync(entity);
        }

        public async Task<IMarginTradingAccount> GetAsync(string clientId, string accountId)
        {
            return await _tableStorage.GetDataAsync(MarginTradingAccountEntity.GeneratePartitionKey(clientId), MarginTradingAccountEntity.GenerateRowKey(accountId));
        }

        public async Task<IMarginTradingAccount> GetAsync(string accountId)
        {
            return (await _tableStorage.GetDataAsync(entity => entity.Id == accountId)).FirstOrDefault();
        }

        public async Task DeleteAsync(string clientId, string accountId)
        {
            await _tableStorage.DeleteAsync(MarginTradingAccountEntity.GeneratePartitionKey(clientId),
                MarginTradingAccountEntity.GenerateRowKey(accountId));
        }
    }
}
