namespace MarginTrading.Core
{
    public interface IClientNotifyService
    {
        void NotifyOrderChanged(Order order);
        void NotifyAccountUpdated(IMarginTradingAccount account);
        void NotifyAccountStopout(string clientId, string accountId, int positionsCount, decimal totalPnl);
    }
}
