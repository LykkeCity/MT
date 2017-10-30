using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Common.Settings.Models;
using MarginTrading.Common.Settings.Repositories.Azure.Entities;

namespace MarginTrading.Common.Settings.Repositories.Azure
{
	public class ClientSettingsRepository : IClientSettingsRepository
	{
		private readonly INoSQLTableStorage<ClientSettingsEntity> _tableStorage;

		public ClientSettingsRepository(INoSQLTableStorage<ClientSettingsEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}

		public async Task<T> GetSettings<T>(string traderId) where T : TraderSettingsBase, new()
		{
			var partitionKey = ClientSettingsEntity.GeneratePartitionKey(traderId);
			var defaultValue = TraderSettingsBase.CreateDefault<T>();
			var rowKey = ClientSettingsEntity.GenerateRowKey(defaultValue);
			var entity = await _tableStorage.GetDataAsync(partitionKey, rowKey);
			return entity == null ? defaultValue : entity.GetSettings<T>();
		}

		public Task SetSettings<T>(string traderId, T settings) where T : TraderSettingsBase, new()
		{
			var newEntity = ClientSettingsEntity.Create(traderId, settings);
			return _tableStorage.InsertOrReplaceAsync(newEntity);
		}
	}   
}
