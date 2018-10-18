using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
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
    }

    public class AccountFpl
    {
        public AccountFpl()
        {
            ActualHash = 1;
        }
        
        public decimal PnL { get; set; }
        public decimal UnrealizedDailyPnl { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal MarginInit { get; set; }
        public int OpenPositionsCount { get; set; }
        public decimal MarginCall1Level { get; set; }
        public decimal MarginCall2Level { get; set; }
        public decimal StopoutLevel { get; set; }

        public decimal WithdrawalFrozenMargin { get; set; }
        public Dictionary<string, decimal> WithdrawalFrozenMarginData { get; set; } = new Dictionary<string, decimal>();
        public decimal UnconfirmedMargin { get; set; }
        public Dictionary<string, decimal> UnconfirmedMarginData { get; set; } = new Dictionary<string, decimal>();

        public int CalculatedHash { get; set; }
        public int ActualHash { get; set; }
    }
}
