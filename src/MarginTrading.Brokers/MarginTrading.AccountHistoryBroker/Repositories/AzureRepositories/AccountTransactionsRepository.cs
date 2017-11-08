using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using MarginTrading.AccountHistoryBroker.Repositories.Models;
using System.Threading.Tasks;

namespace MarginTrading.AccountHistoryBroker.Repositories.AzureRepositories
{
    internal class AccountTransactionsReportsRepository : IAccountTransactionsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountTransactionsReportsEntity> _tableStorage;

        public AccountTransactionsReportsRepository(Settings settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountTransactionsReportsEntity>.Create(() => settings.Db.ReportsConnString,
                "MarginTradingAccountTransactionsReports", log);
        }

        public Task InsertOrReplaceAsync(IAccountTransactionsReport entity)
        {
            return _tableStorage.InsertOrReplaceAsync(AccountTransactionsReportsEntity.Create(entity));
        }
    }
}
