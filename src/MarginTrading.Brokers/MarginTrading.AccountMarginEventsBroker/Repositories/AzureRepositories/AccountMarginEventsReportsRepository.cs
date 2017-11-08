using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.AzureRepositories
{
    internal class AccountMarginEventsReportsRepository : IAccountMarginEventsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountMarginEventReportEntity> _tableStorage;

        public AccountMarginEventsReportsRepository(Settings settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountMarginEventReportEntity>.Create(() => settings.Db.ReportsConnString,
                "AccountMarginEventsReports", log);
        }

        public Task InsertOrReplaceAsync(IAccountMarginEventReport entity)
        {
            return _tableStorage.InsertOrReplaceAsync(AccountMarginEventReportEntity.Create(entity));
        }
    }
}
