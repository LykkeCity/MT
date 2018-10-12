using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AzureRepositories.Entities;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Common.Services;

namespace MarginTrading.AzureRepositories
{
    public class AccountStatRepository : IAccountStatRepository
    {
        private readonly INoSQLTableStorage<AccountStatEntity> _tableStorage;
        private readonly IDateService _dateService;
        
        public AccountStatRepository(IReloadingManager<string> connectionStringManager, 
            ILog log, 
            IDateService dateService)
        {
            _tableStorage = AzureTableStorage<AccountStatEntity>.Create(
                connectionStringManager,
                "AccountStatDump",
                log);
            _dateService = dateService;
        }
        
        public async Task Dump(IEnumerable<MarginTradingAccount> accounts)
        {
            var reportTime = _dateService.Now();
            var entities = accounts.Select(x => AccountStatEntity.Create(x, reportTime));

            await _tableStorage.DeleteAsync();
            await _tableStorage.CreateTableIfNotExistsAsync();
            await _tableStorage.InsertAsync(entities);
        }
    }
}