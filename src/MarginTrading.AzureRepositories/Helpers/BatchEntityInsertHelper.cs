using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories.Helpers
{
    public static class BatchEntityInsertHelper
    {
        public static Task InsertOrReplaceBatchAsync<TEntity>(IEnumerable<TEntity> stats,
            Func<IReadOnlyCollection<TEntity>, Task> saveBatchFunc) where TEntity : ITableEntity
        {
            const int batchSize = 100;
            var tasks = new List<Task>();
            var batch = new Stack<TEntity>(batchSize);
            foreach (var stat in stats.OrderBy(s => s.PartitionKey))
            {
                if (batch.Count >= batchSize ||
                    batch.Count > 0 && batch.Peek().PartitionKey != stat.PartitionKey)
                {
                    tasks.Add(saveBatchFunc(batch));
                    batch.Clear();
                }

                batch.Push(stat);
            }

            if (batch.Count > 0)
            {
                tasks.Add(saveBatchFunc(batch));
            }

            return Task.WhenAll(tasks);
        }
    }
}