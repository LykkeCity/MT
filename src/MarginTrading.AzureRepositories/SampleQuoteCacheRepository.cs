using AzureStorage;
using MarginTrading.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AzureRepositories
{
	public class SampleQuoteCacheRepository : ISampleQuoteCacheRepository
	{
		private readonly INoSQLTableStorage<QuoteEntity> _tableStorage;

		public SampleQuoteCacheRepository(INoSQLTableStorage<QuoteEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}

		public bool Any()
		{
			return _tableStorage.Any();
		}

		public int Count(string instrument, OrderDirection direction)
		{
			return _tableStorage.Count(x => x.PartitionKey == QuoteEntity.GetPartitionKey(instrument, direction));
		}

		public async Task Backup(ISampleQuoteCache hourlyQuoteCache)
		{
			foreach (var queue in hourlyQuoteCache.Buy)
			{
				for (int i = 0; i < queue.Value.Length; i++)
				{
					await _tableStorage.InsertOrReplaceAsync(QuoteEntity.Create(queue.Value[i], i));
				}
			}

			foreach (var queue in hourlyQuoteCache.Sell)
			{
				for (int i = 0; i < queue.Value.Length; i++)
				{
					await _tableStorage.InsertOrReplaceAsync(QuoteEntity.Create(queue.Value[i], i));
				}
			}
		}

		public bool Exists()
		{
			return _tableStorage.Any();
		}

		public async Task<ISampleQuoteCache> Restore()
		{
			SampleQuoteCache cache = new SampleQuoteCache(await GetMaxCount());

			IEnumerable<QuoteEntity> collection = await _tableStorage.GetDataAsync();

			IOrderedEnumerable<QuoteEntity> orderedCollection = collection.OrderBy(x => x.Index);

			foreach (QuoteEntity entity in orderedCollection)
			{
				if (!(entity.PartitionKey == "Settings" && entity.RowKey == "Settings"))
				{
					cache.Enqueue(QuoteEntity.Restore(entity));
				}
			}

			return cache;
		}

		public async Task SaveSettings(int maxCount, long sampleInterval)
		{
			QuoteEntity entity = new QuoteEntity
			{
				PartitionKey = "Settings",
				RowKey = "Settings",
				MaxCount = maxCount,
				SampleInterval = sampleInterval
			};

			await _tableStorage.InsertOrReplaceAsync(entity);
		}

		public async Task<int> GetMaxCount()
		{
			QuoteEntity entity = await _tableStorage.GetDataAsync("Settings", "Settings");

			return entity.MaxCount;
		}

		public async Task<long> GetSampleInterval()
		{
			QuoteEntity entity = await _tableStorage.GetDataAsync("Settings", "Settings");

			return entity.SampleInterval;
		}
	}
}