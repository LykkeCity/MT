using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    public interface IClientNotifyService
    {
        void NotifyOrderChanged(Order order);
        void NotifyAccountUpdated(IMarginTradingAccount account);
        void NotifyAccountStopout(string clientId, string accountId, int positionsCount, decimal totalPnl);
        Task NotifyTradingConditionsChanged(string tradingConditionId = null, string accountId = null);
    }
}
