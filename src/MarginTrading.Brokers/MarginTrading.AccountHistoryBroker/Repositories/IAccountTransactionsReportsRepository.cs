using MarginTrading.AccountHistoryBroker.Repositories.Models;
using System.Threading.Tasks;

namespace MarginTrading.AccountHistoryBroker.Repositories
{
    public interface IAccountTransactionsReportsRepository
    {
        Task InsertOrReplaceAsync(IAccountTransactionsReport entity);
    }
}