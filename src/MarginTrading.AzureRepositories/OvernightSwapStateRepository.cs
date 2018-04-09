using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.AzureRepositories.Entities;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.AzureRepositories
{
	public class OvernightSwapStateRepository : IOvernightSwapStateRepository
	{
		private readonly INoSQLTableStorage<OvernightSwapStateEntity> _tableStorage;
		
		public OvernightSwapStateRepository(INoSQLTableStorage<OvernightSwapStateEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}
		
		public async Task AddOrReplaceAsync(IOvernightSwapState obj)
		{
			await _tableStorage.InsertOrReplaceAsync(OvernightSwapStateEntity.Create(obj));
		}

		public async Task<IEnumerable<IOvernightSwapState>> GetAsync()
		{
			return await _tableStorage.GetDataAsync();
		}

		public async Task DeleteAsync(IOvernightSwapState obj)
		{
			var entity = OvernightSwapStateEntity.Create(obj);
			await _tableStorage.DeleteIfExistAsync(entity.PartitionKey, entity.RowKey);
		}
	}
}