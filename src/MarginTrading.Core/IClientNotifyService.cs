namespace MarginTrading.Core
{
    public interface IClientNotifyService
    {
        void NotifyOrderChanged(Order order);
        void NotifyAccountChanged(IMarginTradingAccount account);
        void NotifyAccountStopout(string clientId, string accountId, int positionsCount, double totalPnl);
    }
}
