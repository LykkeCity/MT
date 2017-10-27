using System;
using System.Threading.Tasks;
using AzureStorage;
using Common;

namespace MarginTrading.AzureRepositories.Logs
{
    public class RiskSystemCommandsLogRepository : IRiskSystemCommandsLogRepository
    {
        private readonly INoSQLTableStorage<RiskSystemCommandsLogEntity> _tableStorage;

        public RiskSystemCommandsLogRepository(INoSQLTableStorage<RiskSystemCommandsLogEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddProcessedAsync(string commandType, object rawCommand)
        {
            var entity = new RiskSystemCommandsLogEntity
            {
                PartitionKey = RiskSystemCommandsLogEntity.GeneratePartitionKey(),
                CommandType = commandType,
                RawCommand = rawCommand?.ToJson(),
                IsError = false,
                Message = null
            };
            await _tableStorage.InsertAndGenerateRowKeyAsTimeAsync(entity, DateTime.UtcNow);
        }
        
        public async Task AddErrorAsync(string commandType, object rawCommand, string errorMessage)
        {
            var entity = new RiskSystemCommandsLogEntity
            {
                PartitionKey = RiskSystemCommandsLogEntity.GeneratePartitionKey(),
                CommandType = commandType,
                RawCommand = rawCommand?.ToJson(),
                IsError = true,
                Message = errorMessage
            };
            await _tableStorage.InsertAndGenerateRowKeyAsTimeAsync(entity, DateTime.UtcNow);
        }
    }
}
