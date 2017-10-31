using MarginTrading.Common.Settings.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.Common.Settings.Repositories.Azure.Entities
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
}