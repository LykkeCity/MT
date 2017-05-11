using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingSettingsService
    {
        Task<bool> IsMargingTradingDemoEnabled(string clientId);
        Task<bool> IsMargingTradingLiveEnabled(string clientId);
    }
}
