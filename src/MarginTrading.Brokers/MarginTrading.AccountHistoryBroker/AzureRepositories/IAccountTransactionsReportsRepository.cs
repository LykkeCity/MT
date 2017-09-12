using System.Threading.Tasks;

namespace MarginTrading.AccountHistoryBroker.AzureRepositories
{
    internal interface IAccountTransactionsReportsRepository
    {
        Task InsertOrReplaceAsync(AccountTransactionsReportsEntity entity);
    }
}