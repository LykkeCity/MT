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
    class AccountsReportsRepository 
    {
        private readonly INoSQLTableStorage<AccountsReportEntity> _tableStorage;

        public AccountsReportsRepository(string connString, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountsReportEntity>.Create(() => connString,
                "ClientAccountsReports", log);
        }
        
        public async Task<IEnumerable<IAccountsReport>> GetData(DateTime dateFrom, DateTime dateTo)
        {
            DateTime dtFrom = dateFrom.Date;
            DateTime dtTo = dateTo.AddDays(1).Date;
            return await _tableStorage.GetDataAsync(e => e.Timestamp >= dtFrom && e.Timestamp < dtTo);
        }
    }
}
