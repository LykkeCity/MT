namespace MarginTrading.Backend.Core
{
    public interface IClientNotifyService
    {
        void NotifyAccountUpdated(IMarginTradingAccount account);
        void NotifyAccountStopout(string clientId, string accountId, int positionsCount, decimal totalPnl);
    }
}
