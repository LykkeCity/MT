using MarginTrading.Core;
using System.Threading.Tasks;

namespace MarginTrading.AccountHistoryBroker.Repositories
{
    internal interface IAccountTransactionsReportsRepository
    {
        Task InsertOrReplaceAsync(IAccountTransactionsReport entity);
    }
}