using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AccountMarginEventsBroker.Repositories.Models;

namespace MarginTrading.AccountMarginEventsBroker.Repositories.AzureRepositories
{
    internal class AccountMarginEventsReportsRepository : IAccountMarginEventsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountMarginEventReportEntity> _tableStorage;

        public AccountMarginEventsReportsRepository(IReloadingManager<Settings> settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountMarginEventReportEntity>.Create(settings.Nested(s => s.Db.ReportsConnString),
                "AccountMarginEventsReports", log);
        }

        public Task InsertOrReplaceAsync(IAccountMarginEventReport entity)
        {
            return _tableStorage.InsertOrReplaceAsync(AccountMarginEventReportEntity.Create(entity));
        }
    }
}
