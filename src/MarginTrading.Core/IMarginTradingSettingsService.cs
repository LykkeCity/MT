using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingSettingsService
    {
        Task<bool> IsMargingTradingEnabled(string clientId);
    }
}
