using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;
using MarginTrading.SqlMigration.Repositories.Azure.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.SqlMigration.Repositories.Azure
{
    internal class AccountMarginEventsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountMarginEventReportEntity> _tableStorage;

        public AccountMarginEventsReportsRepository(string connString, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountMarginEventReportEntity>.Create(() => connString,
                "AccountMarginEventsReports", log);
        }

        public async Task<IEnumerable<IAccountMarginEventReport>> GetData(DateTime dateFrom, DateTime dateTo)
        {
            DateTime dtFrom = dateFrom.Date;
            DateTime dtTo = dateTo.AddDays(1).Date;
            return await _tableStorage.GetDataAsync(e => e.Timestamp >= dtFrom && e.Timestamp < dtTo);
        }
    }
}
