using System;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Common.Services;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class OperationLogEntity : TableEntity, IOperationLog
    {
        public string Name { get; set; }
        public string AccountId { get; set; }
        public string Input { get; set; }
        public string Data { get; set; }

        public static string GeneratePartitionKey(string accountId, string name)
        {
            return accountId ?? name;
        }

        public static OperationLogEntity Create(IOperationLog src)
        {
            return new OperationLogEntity
            {
                PartitionKey = GeneratePartitionKey(src.AccountId, src.Name),
                Name = src.Name,
                Input = src.Input,
                Data = src.Data,
                AccountId = src.AccountId,
            };
        } 
    }

    public class OperationsLogRepository : IOperationsLogRepository
    {
        private readonly INoSQLTableStorage<OperationLogEntity> _tableStorage;

        public OperationsLogRepository(INoSQLTableStorage<OperationLogEntity> tableStorage)
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
