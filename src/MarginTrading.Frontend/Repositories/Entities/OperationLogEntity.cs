using MarginTrading.Common.Services;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.Frontend.Repositories.Entities
{
    public class OperationLogEntity : TableEntity, IOperationLog
    {
        public string Name { get; set; }
        public string ClientId { get; set; }
        public string AccountId { get; set; }
        public string Input { get; set; }
        public string Data { get; set; }

        public static string GeneratePartitionKey(string clientId, string name)
        {
            return clientId ?? name;
        }

        public static OperationLogEntity Create(IOperationLog src)
        {
            return new OperationLogEntity
            {
                PartitionKey = GeneratePartitionKey(src.ClientId, src.Name),
                Name = src.Name,
                Input = src.Input,
                Data = src.Data,
                AccountId = src.AccountId,
                ClientId = src.ClientId
            };
        } 
    }
}