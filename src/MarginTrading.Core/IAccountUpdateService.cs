namespace MarginTrading.Core
{
    public interface IAccountUpdateService
    {
        void UpdateAccount(IMarginTradingAccount account, AccountFpl accountFpl, Order[] orders = null);
        bool IsEnoughBalance(Order order);
        MarginTradingAccount GuessAccountWithOrder(Order order);
    }

    public class AccountFpl
    {
        public decimal PnL { get; set; }
        public decimal UsedMargin { get; set; }
        public decimal MarginInit { get; set; }
        public decimal OpenPositionsCount { get; set; }
        public decimal MarginCall { get; set; }
        public decimal Stopout { get; set; }

        public int CalculatedHash { get; set; }
        public int ActualHash { get; set; }
    }
}
