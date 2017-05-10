using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Core.Clients;
using MarginTrading.Core.Kyc;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories.Clients
{
	public class ClientSettingsEntity : TableEntity
	{
		public static string GeneratePartitionKey(string traderId)
		{
			return traderId;
		}

		public static string GenerateRowKey(TraderSettingsBase settingsBase)
		{
			return settingsBase.GetId();
		}

		public string Data { get; set; }

		internal T GetSettings<T>() where T : TraderSettingsBase
		{
			return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(Data);
		}

		internal void SetSettings(TraderSettingsBase settings)
		{
			Data = Newtonsoft.Json.JsonConvert.SerializeObject(settings);
		}


		public static ClientSettingsEntity Create(string traderId, TraderSettingsBase settings)
		{
			var result = new ClientSettingsEntity
			{
				PartitionKey = GeneratePartitionKey(traderId),
				RowKey = GenerateRowKey(settings),
			};
			result.SetSettings(settings);
			return result;
		}
	}


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

		public Task DeleteAsync<T>(string traderId) where T : TraderSettingsBase, new()
		{
			var partitionKey = ClientSettingsEntity.GeneratePartitionKey(traderId);
			var defaultValue = TraderSettingsBase.CreateDefault<T>();
			var rowKey = ClientSettingsEntity.GenerateRowKey(defaultValue);
			return _tableStorage.DeleteAsync(partitionKey, rowKey);
		}

		public async Task UpdateKycDocumentSettingOnUpload(string clientId, string modelType)
		{
			if (!modelType.HasDocumentType())
				return;

			var clientKycSettings = await GetSettings<KycProfileSettings>(clientId);
			switch (modelType)
			{
				case KycDocumentTypes.IdCard:
					clientKycSettings.ShowIdCard = false;
					break;
				case KycDocumentTypes.ProofOfAddress:
					clientKycSettings.ShowIdProofOfAddress = false;
					break;
				case KycDocumentTypes.Selfie:
					clientKycSettings.ShowSelfie = false;
					break;
			}
			await SetSettings(clientId, clientKycSettings);
		}
	}   
}
