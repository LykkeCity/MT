using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AccountHistoryBroker.Repositories.Models;

namespace MarginTrading.AccountHistoryBroker.Repositories.AzureRepositories
{
    internal class AccountTransactionsReportsRepository : IAccountTransactionsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountTransactionsReportsEntity> _tableStorage;

        public AccountTransactionsReportsRepository(IReloadingManager<Settings> settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountTransactionsReportsEntity>.Create(settings.Nested(s => s.Db.ReportsConnString),
                "MarginTradingAccountTransactionsReports", log);
        }

        public Task InsertOrReplaceAsync(IAccountTransactionsReport entity)
        {
            return _tableStorage.InsertOrReplaceAsync(AccountTransactionsReportsEntity.Create(entity));
        }
    }
}
