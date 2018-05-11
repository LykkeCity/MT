using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Refit;

namespace MarginTrading.Backend.Contracts.Snow.Trades
{
    /// <summary>                                                                                       
    /// Provides data about trades
    /// </summary>
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