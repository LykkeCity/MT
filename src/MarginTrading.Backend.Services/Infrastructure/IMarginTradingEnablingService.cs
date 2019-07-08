// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MarginTrading.Backend.Services
{
    public interface IMarginTradingEnablingService
    {
        /// <summary>
        /// Enables or disables margin trading for specified <paramref name="clientId"/>
        /// </summary>
        Task SetMarginTradingEnabled(string clientId, bool enabled);
    }
}