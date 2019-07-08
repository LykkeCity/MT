// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories.Helpers
{
    public static class BatchEntityInsertHelper
    {
        /// <summary>
        /// Splits <paramref name="entities"/> into batches with max size of 100.
        /// Each batch contains single PartitionKey.
        /// </summary>
        public static IEnumerable<IReadOnlyCollection<TEntity>> MakeBatchesByPartitionKey<TEntity>(IEnumerable<TEntity> entities)
            where TEntity : ITableEntity
        {
            if (entities == null)
            {
                throw new ArgumentNullException(nameof(entities));
            }

            const int batchSize = 100;
            var batch = new Stack<TEntity>(batchSize);
            foreach (var stat in entities.OrderBy(s => s.PartitionKey))
            {
                if (batch.Count >= batchSize ||
                    batch.Count > 0 && batch.Peek().PartitionKey != stat.PartitionKey)
                {
                    yield return batch;
                    batch = new Stack<TEntity>(batchSize);
                }

                batch.Push(stat);
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }
    }
}