using System.Threading.Tasks;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core.Services
{
    public interface IAccountUpdateService
    {
        void UpdateAccount(IMarginTradingAccount account);
        Task FreezeWithdrawalMargin(string accountId, string operationId, decimal amount);
        Task UnfreezeWithdrawalMargin(string accountId, string operationId);
        Task FreezeUnconfirmedMargin(string accountId, string operationId, decimal amount);
        Task UnfreezeUnconfirmedMargin(string accountId, string operationId);
        bool IsEnoughBalance(Order order);
        void RemoveLiquidationStateIfNeeded(string accountId, string reason,
            string liquidationOperationId = null);

        decimal CalculateOvernightUsedMargin(IMarginTradingAccount account);
        void RemoveDeleted(string accountId);
    }
}
