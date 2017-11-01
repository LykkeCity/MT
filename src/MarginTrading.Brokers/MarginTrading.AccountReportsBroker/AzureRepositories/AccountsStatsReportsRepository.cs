using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AccountReportsBroker.AzureRepositories.Entities;
using MarginTrading.AzureRepositories.Helpers;

namespace MarginTrading.AccountReportsBroker.AzureRepositories
{
    public class AccountsStatsReportsRepository : IAccountsStatsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountsStatReportEntity> _tableStorage;

        public AccountsStatsReportsRepository(Settings settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountsStatReportEntity>.Create(() => settings.Db.ReportsConnString,
                "ClientAccountsStatusReports", log);
        }
        
        public Task InsertOrReplaceBatchAsync(IEnumerable<AccountsStatReportEntity> stats)
        {
            var tasks = BatchEntityInsertHelper.MakeBatchesByPartitionKey(stats)
                .Select(b => _tableStorage.InsertOrReplaceBatchAsync(b));
            return Task.WhenAll(tasks);
        }
    }
}
