using System;
using Lykke.AzureStorage.Tables;
using MarginTrading.Backend.Core;
using Newtonsoft.Json;

namespace MarginTrading.AzureRepositories.Entities
{
    public class OperationExecutionInfoEntity : AzureTableEntity, IOperationExecutionInfo<object>
    {
        public string OperationName
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }
        
        public string Id
        {
            get => RowKey;
            set => RowKey = value;
        }

        public DateTime LastModified { get; set; }

        object IOperationExecutionInfo<object>.Data => JsonConvert.DeserializeObject<object>(Data);
        public string Data { get; set; }

        public static string GeneratePartitionKey(string operationName)
        {
            return operationName;
        }

        public static string GenerateRowKey(string id)
        {
            return id;
        }
    }
}