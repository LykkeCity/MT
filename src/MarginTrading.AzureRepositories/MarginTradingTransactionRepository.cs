using AzureStorage;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingTransactionRepository : IMarginTradingTransactionRepository
    {
        private readonly INoSQLTableStorage<MarginTradingTransactionEntity> _tableStorage;

        public MarginTradingTransactionRepository(INoSQLTableStorage<MarginTradingTransactionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddAsync(ITransaction transaction)
        {
            var entity = MarginTradingTransactionEntity.Create(transaction);
			await _tableStorage.InsertAsync(entity);
        }

        public bool Any()
        {
            return _tableStorage.Any();
        }

        public async Task<IEnumerable<ITransaction>> GetTransactionsAsync(DateTime? from = default(DateTime?), DateTime? to = default(DateTime?))
        {
            var result = await _tableStorage.GetDataAsync(x => (from != null ? x.ExecutedTime >= from : true) && (to != null ? x.ExecutedTime <= to : true));

            return result.Select(MarginTradingTransactionEntity.Restore);
        }

        public async Task<IEnumerable<ITransaction>> GetTransactionsByMarketMakerAsync(string marketMakerId, string[] assets, DateTime? from = default(DateTime?), DateTime? to = default(DateTime?))
        {
            var result = await _tableStorage.GetDataAsync(
                partitionKeys: assets.Select(x => MarginTradingTransactionEntity.GeneratePartitionKey(marketMakerId, x)).ToArray(),
                filter: x => (from != null ? x.ExecutedTime >= from : true) && (to != null ? x.ExecutedTime <= to : true));

            return result.Select(MarginTradingTransactionEntity.Restore);
        }
    }
}
