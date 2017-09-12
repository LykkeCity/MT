using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;

namespace MarginTrading.AccountHistoryBroker.AzureRepositories
{
    internal class AccountTransactionsReportsRepository : IAccountTransactionsReportsRepository
    {
        private readonly INoSQLTableStorage<AccountTransactionsReportsEntity> _tableStorage;

        public AccountTransactionsReportsRepository(Settings settings, ILog log)
        {
            _tableStorage = AzureTableStorage<AccountTransactionsReportsEntity>.Create(() => settings.MtReportsConnectionString,
                "AccountTransactions", log);
        }

        public Task InsertOrReplaceAsync(AccountTransactionsReportsEntity entity)
        {
            return _tableStorage.InsertOrReplaceAsync(entity);
        }
    }
}
