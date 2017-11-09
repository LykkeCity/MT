using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AccountReportsBroker.Repositories.Models;
using MarginTrading.SqlMigration.Repositories.Azure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.SqlMigration.Repositories.Azure
{
    class AccountsStatsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountsStatReportEntity> _tableStorage;

        public AccountsStatsReportsRepository(string connString, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountsStatReportEntity>.Create(() => connString,
                "ClientAccountsStatusReports", log); 
        }

        public async Task<IEnumerable<IAccountsStatReport>> GetData(DateTime dateFrom, DateTime dateTo)
        {
            DateTime dtFrom = dateFrom.Date;
            DateTime dtTo = dateTo.AddDays(1).Date;
            return await _tableStorage.GetDataAsync(e => e.Timestamp >= dtFrom && e.Timestamp < dtTo);
        }
    }
}
