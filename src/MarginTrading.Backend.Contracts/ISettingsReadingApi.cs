using JetBrains.Annotations;
using Refit;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface ISettingsReadingApi
    {
        /// <summary>
        /// Returns if margin trading is enabled for client
        /// </summary>
        [Get("/api/settings/enabled/{clientId}")]
        Task<bool> IsMarginTradingEnabled(string clientId);
    }
}
