using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Trades;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>                                                                                       
    /// Provides data about trades
    /// </summary>
    // todo move to history
    [PublicAPI]
    public interface ITradesApi
    {
        /// <summary>
        /// Get a trade by id
        /// </summary>
        [Get("/api/trades/{tradeId}"), ItemCanBeNull]
        Task<TradeContract> Get(string tradeId);

        /// <summary>
        /// Get trades with optional filtering by order or position 
        /// </summary>
        [Get("/api/trades/")]
        Task<List<TradeContract>> List([Query, CanBeNull] string orderId, [Query, CanBeNull] string positionId);
    }
}