using System.Threading.Tasks;
using MarginTrading.Common.Services.Settings;

namespace MarginTrading.Backend.Services.Services
{
    /// <summary>
    /// Detects if margin trading of particular types (live and demo) is available globally and for user.
    /// </summary>
    public interface IMarginTradingSettingsCacheService
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
        /// Detects if margin trading for <paramref name="accountId"/>
        /// </summary>
        bool? IsMarginTradingEnabledByAccountId(string accountId);

        /// <summary>
        /// Notifies the service of availability change 
        /// </summary>
        void OnMarginTradingEnabledChanged(MarginTradingEnabledChangedMessage message);
    }
}
