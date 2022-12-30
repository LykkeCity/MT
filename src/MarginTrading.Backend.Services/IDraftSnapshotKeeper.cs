// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Services
{
    
    /// <summary>
    /// Keeper of snapshot draft within the scope of current request
    /// </summary>
    public interface IDraftSnapshotKeeper : IOrderReader
    {
        /// <summary>
        /// The trading day draft trading snapshot is being kept for
        /// <exception cref="InvalidOperationException">
        /// When keeper has not been initialized yet
        /// </exception>
        /// </summary>
        DateTime TradingDay { get; }
        
        /// <summary>
        /// The timestamp of the snapshot (when it was created).
        /// The timestamp of the Final snapshot will have the timestamp of Draft
        /// it was created upon
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// The list of fx prices
        /// </summary>
        List<BestPriceContract> FxPrices { get; }
        
        /// <summary>
        /// The list of cfd quotes
        /// </summary>
        List<BestPriceContract> CfdQuotes { get; }

        /// <summary>
        /// Keeper initialization with trading day. Required to be called before keeper usage
        /// </summary>
        /// <param name="tradingDay">The trading day draft snapshot to be kept for</param>
        IDraftSnapshotKeeper Init(DateTime tradingDay);
        
        /// <summary>
        /// Checks if draft trading snapshot exists for the trading day
        /// </summary>
        /// <returns></returns>
        ValueTask<bool> ExistsAsync();

        /// <summary>
        /// Fetches account list from draft trading snapshot
        /// </summary>
        /// <returns></returns>
        ValueTask<List<MarginTradingAccount>> GetAccountsAsync();


        /// <summary>
        /// Updates draft trading snapshot in-memory only, no persistence. Orders, positions and accounts are being
        /// replaced whereas fx rates and cfd quotes are being merged in.
        /// </summary>
        /// <param name="positions">The list of positions</param>
        /// <param name="orders">The list of orders</param>
        /// <param name="accounts">The list of accounts</param>
        /// <param name="fxRates">The list of fx rates</param>
        /// <param name="cfdQuotes">The list of cfd quotes</param>
        /// <returns></returns>
        Task UpdateAsync(ImmutableArray<Position> positions,
            ImmutableArray<Order> orders,
            ImmutableArray<MarginTradingAccount> accounts,
            IEnumerable<BestPriceContract> fxRates,
            IEnumerable<BestPriceContract> cfdQuotes);
    }
}