using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using MarginTrading.Core;
using MarginTrading.Core.Helpers;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingAccountHistoryEntity : TableEntity, IMarginTradingAccountHistory
    {
        public string Id { get; set; }
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
        public int? EntityVersion { get; set; }
        AccountHistoryType IMarginTradingAccountHistory.Type => Type.ParseEnum(AccountHistoryType.OrderClosed);

        public static string GeneratePartitionKey(string accountId)
        {
            return accountId;
        }

        public static MarginTradingAccountHistoryEntity Create(IMarginTradingAccountHistory src)
        {
            return new MarginTradingAccountHistoryEntity
            {
                Id = src.Id,
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
            var entity = MarginTradingAccountHistoryEntity.Create(accountHistory);
            entity.EntityVersion = 2;
            // ReSharper disable once RedundantArgumentDefaultValue
            await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity, entity.Date, RowKeyDateTimeFormat.Iso);
        }

        public async Task<IReadOnlyList<IMarginTradingAccountHistory>> GetAsync(string[] accountIds, DateTime? from,
            DateTime? to)
        {
            return (await _tableStorage.WhereAsync(accountIds, from ?? DateTime.MinValue, to ?? DateTime.MaxValue, ToIntervalOption.IncludeTo))
                .OrderByDescending(item => item.RowKey).ToList();
        }
    }
}
