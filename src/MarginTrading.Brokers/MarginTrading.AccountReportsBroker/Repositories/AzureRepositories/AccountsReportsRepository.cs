using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AccountReportsBroker.Repositories.AzureRepositories.Entities;
using MarginTrading.AccountReportsBroker.Repositories.Models;

namespace MarginTrading.AccountReportsBroker.Repositories.AzureRepositories
{
    public class AccountsReportsRepository : IAccountsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountsReportEntity> _tableStorage;

        public AccountsReportsRepository(IReloadingManager<Settings> settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountsReportEntity>.Create(settings.Nested(s => s.Db.ReportsConnString),
                "ClientAccountsReports", log);
        }

        public Task InsertOrReplaceAsync(IAccountsReport report)
        {
            return _tableStorage.InsertOrReplaceAsync(AccountsReportEntity.Create(report));
        }
    }
}
