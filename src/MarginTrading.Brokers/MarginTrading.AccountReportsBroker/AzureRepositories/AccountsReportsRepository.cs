using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AccountReportsBroker.AzureRepositories.Entities;

namespace MarginTrading.AccountReportsBroker.AzureRepositories
{
    public class AccountsReportsRepository : IAccountsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountsReportEntity> _tableStorage;

        public AccountsReportsRepository(Settings settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountsReportEntity>.Create(() => settings.Db.ReportsConnString,
                "ClientAccountsReports", log);
        }

        public Task InsertOrReplaceAsync(AccountsReportEntity report)
        {
            return _tableStorage.InsertOrReplaceAsync(report);
        }
    }
}
