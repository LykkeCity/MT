using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.AzureRepositories.Entities;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using MoreLinq;

namespace MarginTrading.AzureRepositories
{
	public class OvernightSwapHistoryRepository : IOvernightSwapHistoryRepository
	{
		private readonly INoSQLTableStorage<OvernightSwapHistoryEntity> _tableStorage;
		
		public OvernightSwapHistoryRepository(INoSQLTableStorage<OvernightSwapHistoryEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}
		
		public async Task AddAsync(IOvernightSwapHistory obj)
		{
			var entity = OvernightSwapHistoryEntity.Create(obj);
			await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity, entity.Timestamp);
		}

		public async Task<IEnumerable<IOvernightSwapHistory>> GetAsync()
		{
			return await _tableStorage.GetDataAsync();
		}

		public async Task<IReadOnlyList<IOvernightSwapHistory>> GetAsync(DateTime? @from, DateTime? to)
		{
			var partitionKeys = await GetPartitionKeys();
			return (await _tableStorage.WhereAsync(partitionKeys, from ?? DateTime.MinValue, to ?? DateTime.MaxValue, ToIntervalOption.IncludeTo))
				.OrderByDescending(item => item.Timestamp)
				.ToList();
		}

		public async Task<IReadOnlyList<IOvernightSwapHistory>> GetAsync(string accountId, DateTime? @from, DateTime? to)
		{
			return (await _tableStorage.WhereAsync(accountId, from ?? DateTime.MinValue, to ?? DateTime.MaxValue, 
					ToIntervalOption.IncludeTo))
				.OrderByDescending(item => item.Timestamp).ToList();
		}

		private async Task<IEnumerable<string>> GetPartitionKeys()
		{
			var partitionKeys = new ConcurrentBag<string>();
			await _tableStorage.ExecuteAsync(new TableQuery<OvernightSwapHistoryEntity>(), entity =>
				entity.Select(m => m.PartitionKey).ForEach(pk => partitionKeys.Add(pk)));
			return partitionKeys.Distinct();
		}
	}
}