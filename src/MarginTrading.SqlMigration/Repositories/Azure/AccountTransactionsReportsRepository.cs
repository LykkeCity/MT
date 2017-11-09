using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AccountHistoryBroker.Repositories.Models;
using MarginTrading.SqlMigration.Repositories.Azure.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.SqlMigration.Repositories.Azure
{
    // AccountHistoryBroker
    internal class AccountTransactionsReportsRepository 
    {
        private readonly INoSQLTableStorage<AccountTransactionsReportsEntity> _tableStorage;

        public AccountTransactionsReportsRepository(string connString, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountTransactionsReportsEntity>.Create(() => connString,
                "MarginTradingAccountTransactionsReports", log);
        }

        public async Task<IEnumerable<IAccountTransactionsReport>> GetData(DateTime dateFrom, DateTime dateTo)
        {
            DateTime dtFrom = dateFrom.Date;
            DateTime dtTo = dateTo.AddDays(1).Date;
            return await _tableStorage.GetDataAsync(e => e.Timestamp >= dtFrom && e.Timestamp < dtTo);
        }
    }
}
