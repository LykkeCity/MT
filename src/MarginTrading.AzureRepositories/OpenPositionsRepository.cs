using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories.Entities;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Common.Services;

namespace MarginTrading.AzureRepositories
{
    public class OpenPositionsRepository : IOpenPositionsRepository
    {
        private readonly INoSQLTableStorage<OpenPositionEntity> _tableStorage;
        private readonly IDateService _dateService;
        
        public OpenPositionsRepository(IReloadingManager<string> connectionStringManager, 
            ILog log, 
            IDateService dateService)
        {
            _tableStorage = AzureTableStorage<OpenPositionEntity>.Create(
                connectionStringManager,
                "OpenPositionsDump",
                log);
            _dateService = dateService;
        }
        
        public async Task Dump(IEnumerable<Position> openPositions)
        {
            var reportTime = _dateService.Now();
            var entities = openPositions.Select(x => OpenPositionEntity.Create(x, reportTime));

            await _tableStorage.DeleteAsync();
            await _tableStorage.CreateTableIfNotExistsAsync();
            await _tableStorage.InsertAsync(entities);
        }
    }
}