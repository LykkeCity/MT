namespace MarginTrading.Backend.Core
{
    public interface IClientNotifyService
    {
        void NotifyAccountUpdated(IMarginTradingAccount account);
    }
}
