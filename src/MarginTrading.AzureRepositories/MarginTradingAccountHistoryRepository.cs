using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingAccountHistoryEntity : TableEntity, IMarginTradingAccountHistory
    {
        public string Id => RowKey;
        public DateTime Date { get; set; }
        public string AccountId => PartitionKey;
        public string ClientId { get; set; }
        decimal IMarginTradingAccountHistory.Amount => (decimal) Amount;
        public double Amount { get; set; }
        decimal IMarginTradingAccountHistory.Balance => (decimal) Balance;
        public double Balance { get; set; }
        decimal IMarginTradingAccountHistory.WithdrawTransferLimit => (decimal) WithdrawTransferLimit;
        public double WithdrawTransferLimit { get; set; }
        public string Comment { get; set; }
        public string Type { get; set; }
        AccountHistoryType IMarginTradingAccountHistory.Type => Type.ParseEnum(AccountHistoryType.OrderClosed);

        public static string GeneratePartitionKey(string accountId)
        {
            return accountId;
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }

        public static MarginTradingAccountHistoryEntity Create(IMarginTradingAccountHistory src)
        {
            return new MarginTradingAccountHistoryEntity
            {
                RowKey = GenerateRowKey(src.Id),
                PartitionKey = GeneratePartitionKey(src.AccountId),
                Date = src.Date,
                ClientId = src.ClientId,
                Amount = (double) src.Amount,
                Balance = (double) src.Balance,
                WithdrawTransferLimit = (double) src.WithdrawTransferLimit,
                Comment = src.Comment,
                Type = src.Type.ToString()
            };
        }
    }

    public class MarginTradingAccountHistoryRepository : IMarginTradingAccountHistoryRepository
    {
        private readonly INoSQLTableStorage<MarginTradingAccountHistoryEntity> _tableStorage;

        public MarginTradingAccountHistoryRepository(INoSQLTableStorage<MarginTradingAccountHistoryEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddAsync(IMarginTradingAccountHistory accountHistory)
        {
            await _tableStorage.InsertOrReplaceAsync(MarginTradingAccountHistoryEntity.Create(accountHistory));
        }

        public async Task<IEnumerable<IMarginTradingAccountHistory>> GetAsync(string[] accountIds, DateTime? from,
            DateTime? to)
        {
            var entities = await _tableStorage.GetDataAsync(
                entity => accountIds.Contains(entity.AccountId) && (entity.Date >= from || from == null) &&
                          (entity.Date <= to || to == null));
            return entities.Select(MarginTradingAccountHistory.Create).OrderByDescending(item => item.Date);
        }
    }
}
