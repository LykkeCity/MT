using System.Threading.Tasks;

namespace MarginTrading.Backend.Services
{
    public interface IMarginTradingEnablingService
    {
        /// <summary>
        /// Enables or disables margin trading of specified type <paramref name="isLive"/>
        /// for specified <paramref name="clientId"/>
        /// </summary>
        Task SetMarginTradingEnabled(string clientId, bool isLive, bool enabled);
    }
}