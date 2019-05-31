using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Orders;
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
        void CheckIsEnoughBalance(Order order, IMatchingEngineBase matchingEngine);
        void RemoveLiquidationStateIfNeeded(string accountId, string reason,
            string liquidationOperationId = null, LiquidationType liquidationType = LiquidationType.Normal);

        decimal CalculateOvernightUsedMargin(IMarginTradingAccount account);
    }
}
