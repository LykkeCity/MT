using System;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Backend.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
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

    public class MarginTradingOperationsLogRepository : IMarginTradingOperationsLogRepository
    {
        private readonly INoSQLTableStorage<OperationLogEntity> _tableStorage;

        public MarginTradingOperationsLogRepository(INoSQLTableStorage<OperationLogEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddLogAsync(IOperationLog logEntity)
        {
            var entity = OperationLogEntity.Create(logEntity);
            await _tableStorage.InsertAndGenerateRowKeyAsTimeAsync(entity, DateTime.UtcNow);
        }
    }
}
