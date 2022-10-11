// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// Api to manage service functions
    /// </summary>
    [PublicAPI]
    public interface IServiceApi
    {
        /// <summary>
        /// Save snapshot of orders, positions, account stats, best fx prices, best trading prices for current moment.
        /// Throws an error in case if trading is not stopped.
        /// </summary>
        /// <returns>Snapshot statistics.</returns>
        [Post("/api/service/make-trading-data-snapshot")]
        Task<string> MakeTradingDataSnapshot([Query] DateTime tradingDay, 
            [Query, CanBeNull] string correlationId = null);
        
        /// <summary>
        /// Get current state of overnight margin parameter.
        /// </summary>
        [Get("/api/service/current-overnight-margin-parameter")]
        Task<bool> GetOvernightMarginParameterCurrentState();

        /// <summary>
        /// Get current margin parameter values for instruments (all / filtered by IDs).
        /// </summary>
        /// <returns>
        /// Dictionary with key = asset pair ID and value = (Dictionary with key = trading condition ID and value = multiplier)
        /// </returns>
        [Get("/api/service/overnight-margin-parameter")]
        Task<Dictionary<string, Dictionary<string, decimal>>> GetOvernightMarginParameterValues(
            [Query, CanBeNull] string[] instruments = null);
        
        /// <summary>
        /// Get unconfirmed margin current state for the account
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [Get("/api/service/unconfirmed-margin")]
        Dictionary<string, decimal> GetUnconfirmedMargin([Query] string accountId);
        
        /// <summary>
        /// Freezes amount if margin attached to operationId and account
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="operationId"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        [Post("/api/service/unconfirmed-margin")]
        Task FreezeUnconfirmedMargin([Query] string accountId, [Query] string operationId, [Query] decimal amount);
        
        /// <summary>
        /// Unfreezes amount of margin attached to operationId and account
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="operationId"></param>
        [Delete("/api/service/unconfirmed-margin")]
        Task UnfreezeUnconfirmedMargin([Query] string accountId, [Query] string operationId);
    }
}