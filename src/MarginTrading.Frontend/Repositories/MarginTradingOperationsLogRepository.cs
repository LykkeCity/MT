using System;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.Common.Services;
using MarginTrading.Frontend.Repositories.Entities;

namespace MarginTrading.Frontend.Repositories
{
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
