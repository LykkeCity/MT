using System.Threading.Tasks;
using MarginTrading.Core.Models;

namespace MarginTrading.Core
{
    /// <summary>
    /// Detects if margin trading of particular types (live and demo) is available globally and for user.
    /// </summary>
    public interface IMarginTradingSettingsService
    {
        /// <summary>
        /// Detects if margin trading of particular types is available globally and for user <paramref name="clientId"/>
        /// </summary>
        Task<EnabledMarginTradingTypes> IsMarginTradingEnabled(string clientId);

        /// <summary>
        /// Detects if margin trading of specified in <paramref name="isLive"/> type is available globally and for user <paramref name="clientId"/>
        /// </summary>
        Task<bool> IsMarginTradingEnabled(string clientId, bool isLive);

        /// <summary>
        /// Enables or disables margin trading of specified type <paramref name="isLive"/> for specified <paramref name="clientId"/>
        /// </summary>
        Task SetMarginTradingEnabled(string clientId, bool isLive, bool enabled);

        /// <summary>
        /// Removes from cache value for user
        /// </summary>
        void ResetCacheForClient(string clientId);
    }
}
