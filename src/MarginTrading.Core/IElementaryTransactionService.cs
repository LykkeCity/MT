using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IElementaryTransactionService
    {
        Task CreateElementaryTransactionsAsync(ITransaction transaction);

        Task CreateElementaryTransactionsFromTransactionReport();
        bool Any();
    }
}