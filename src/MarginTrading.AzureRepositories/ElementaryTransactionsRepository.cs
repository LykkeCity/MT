using AzureStorage;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AzureRepositories
{
    public class ElementaryTransactionsRepository : IElementaryTransactionsRepository
    {
        private readonly INoSQLTableStorage<ElementaryTransactionEntity> _tableStorage;

        public ElementaryTransactionsRepository(INoSQLTableStorage<ElementaryTransactionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddAsync(IElementaryTransaction elementaryTransaction)
        {
            var entity = ElementaryTransactionEntity.Create(elementaryTransaction);
            await _tableStorage.InsertOrMergeAsync(entity);
        }

        public bool Any()
        {
            return _tableStorage.Any();
        }

        public async Task<IEnumerable<IElementaryTransaction>> GetTransactionsAsync(DateTime? from = default(DateTime?), DateTime? to = default(DateTime?))
        {
            var result = await _tableStorage.GetDataAsync(x => (from != null ? x.Timestamp >= from : true) && (to != null ? x.Timestamp <= to : true));

            return result.Select(ElementaryTransactionEntity.Restore);
        }

        public async Task<IEnumerable<IElementaryTransaction>> GetTransactionsByCounterPartyAsync(string counterParty, string[] assets, DateTime? from = default(DateTime?), DateTime? to = default(DateTime?))
        {
            var result = await _tableStorage.GetDataAsync(
                partitionKeys: assets.Select(x => ElementaryTransactionEntity.GeneratePartitionKey(new ElementaryTransaction { CounterPartyId = counterParty, Asset = x })).ToArray(),
                filter: x => (from != null ? x.TimeStamp >= from : true) && (to != null ? x.TimeStamp <= to : true));

            return result.Select(ElementaryTransactionEntity.Restore);
        }
    }
}