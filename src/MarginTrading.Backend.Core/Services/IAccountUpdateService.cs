using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
    public interface IAccountUpdateService
    {
        void UpdateAccount(IMarginTradingAccount account);
        void FreezeWithdrawalMargin(string accountId, string operationId, decimal amount);
        void UnfreezeWithdrawalMargin(string accountId, string operationId);
        bool IsEnoughBalance(Order order);
        MarginTradingAccount GuessAccountWithNewActiveOrder(Position order);
    }

    public class AccountFpl
    {
        public AccountFpl()
        {
            ActualHash = 1;
        }
        
        public decimal PnL { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal MarginInit { get; set; }
        public int OpenPositionsCount { get; set; }
        public decimal MarginCallLevel { get; set; }
        public decimal StopoutLevel { get; set; }

        public decimal WithdrawalFrozenMargin { get; set; }
        public Dictionary<string, decimal> WithdrawalFrozenMarginData { get; set; } = new Dictionary<string, decimal>(); 

        public int CalculatedHash { get; set; }
        public int ActualHash { get; set; }
    }
}
