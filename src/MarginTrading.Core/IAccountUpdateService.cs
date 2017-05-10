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
        public double PnL { get; set; }
        public double UsedMargin { get; set; }
        public double MarginInit { get; set; }
        public double OpenPositionsCount { get; set; }
        public double MarginCall { get; set; }
        public double Stopout { get; set; }

        public int CalculatedHash { get; set; }
        public int ActualHash { get; set; }
    }
}
