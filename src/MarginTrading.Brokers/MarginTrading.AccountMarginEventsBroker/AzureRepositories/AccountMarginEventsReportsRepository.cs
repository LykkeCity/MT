﻿using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;

namespace MarginTrading.AccountMarginEventsBroker.AzureRepositories
{
    internal class AccountMarginEventsReportsRepository : IAccountMarginEventsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountMarginEventReportEntity> _tableStorage;

        public AccountMarginEventsReportsRepository(Settings settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountMarginEventReportEntity>.Create(() => settings.Db.ReportsConnString,
                "AccountMarginEventsReports", log);
        }

        public Task InsertOrReplaceAsync(AccountMarginEventReportEntity entity)
        {
            return _tableStorage.InsertOrReplaceAsync(entity);
        }
    }
}
