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
			await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity, entity.Time);
		}

		public async Task<IEnumerable<IOvernightSwapHistory>> GetAsync()
		{
			return await _tableStorage.GetDataAsync();
		}

		public async Task<IReadOnlyList<IOvernightSwapHistory>> GetAsync(DateTime? @from, DateTime? to)
		{
			return (await _tableStorage.WhereAsync(AzureStorageUtils.QueryGenerator<OvernightSwapHistoryEntity>.RowKeyOnly
					.BetweenQuery(from ?? DateTime.MinValue, to ?? DateTime.MaxValue, ToIntervalOption.IncludeTo)))
				.OrderByDescending(item => item.Time)
				.ToList();
		}

		public async Task<IReadOnlyList<IOvernightSwapHistory>> GetAsync(string accountId, DateTime? @from, DateTime? to)
		{
			return (await _tableStorage.WhereAsync(accountId, from ?? DateTime.MinValue, to ?? DateTime.MaxValue, 
					ToIntervalOption.IncludeTo))
				.OrderByDescending(item => item.Time).ToList();
		}

		public async Task DeleteAsync(IOvernightSwapHistory obj)
		{
			await _tableStorage.DeleteAsync(OvernightSwapHistoryEntity.Create(obj));
		}
	}
}