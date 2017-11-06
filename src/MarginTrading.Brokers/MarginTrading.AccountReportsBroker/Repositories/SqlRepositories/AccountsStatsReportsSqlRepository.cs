using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AccountReportsBroker.Repositories;
using MarginTrading.AccountReportsBroker.Repositories.AzureRepositories.Entities;
using MarginTrading.AccountReportsBroker.Repositories.Models;
using MarginTrading.AzureRepositories.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AccountReportsBroker.AzureRepositories
{
    public class AccountsStatsReportsSqlRepository : IAccountsStatsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountsStatReportEntity> _tableStorage;

        public AccountsStatsReportsSqlRepository(Settings settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountsStatReportEntity>.Create(() => settings.Db.ReportsConnString,
                "ClientAccountsStatusReports", log);
        }
        
        public Task InsertOrReplaceBatchAsync(IEnumerable<IAccountsStatReport> stats)
        {
            throw new System.NotImplementedException();
        }
    }
}
