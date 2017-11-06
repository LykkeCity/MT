using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AccountReportsBroker.Repositories.AzureRepositories.Entities;
using MarginTrading.AccountReportsBroker.Repositories.Models;
using MarginTrading.AzureRepositories.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AccountReportsBroker.Repositories.AzureRepositories
{
    public class AccountsStatsReportsRepository : IAccountsStatsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountsStatReportEntity> _tableStorage;

        public AccountsStatsReportsRepository(Settings settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountsStatReportEntity>.Create(() => settings.Db.ReportsConnString,
                "ClientAccountsStatusReports", log);
        }
        
        public Task InsertOrReplaceBatchAsync(IEnumerable<IAccountsStatReport> stats)
        {
            var tasks = BatchEntityInsertHelper.MakeBatchesByPartitionKey(stats.Select(m => AccountsStatReportEntity.Create(m)))
                .Select(b => _tableStorage.InsertOrReplaceBatchAsync(b));
            return Task.WhenAll(tasks);
        }
    }
}
