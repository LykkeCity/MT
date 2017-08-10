using System.Threading.Tasks;

namespace MarginTrading.Core
{
    /// <summary>
    /// Detects if margin trading of specified type (live or demo) is available globally and for user.
    /// </summary>
    public interface IMarginTradingSettingsService
    {
        /// <summary>
        /// Detects if demo margin trading is available globally and for user <paramref name="clientId"/>
        /// </summary>
        Task<bool> IsMarginTradingDemoEnabled(string clientId);

        /// <summary>
        /// Detects if live margin trading is available globally and for user <paramref name="clientId"/>
        /// </summary>
        Task<bool> IsMarginTradingLiveEnabled(string clientId);

        /// <summary>
        /// Detects if margin trading of specified in <paramref name="isLive"/> type is available globally and for user <paramref name="clientId"/>
        /// </summary>
        Task<bool> IsMarginTradingEnabled(string clientId, bool isLive);

        /// <summary>
        /// Enables or disables margin trading of specified type <paramref name="isLive"/> for specified <paramref name="clientId"/>
        /// </summary>
        Task SetMarginTradingEnabled(string clientId, bool isLive, bool enabled);
    }
}
