using JetBrains.Annotations;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.TradeMonitoring;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface ITradeMonitoringReadingApi
    {
        /// <summary>
        /// Returns summary info by assets
        /// </summary>
        [Get("/api/trade/assets/summary/")]
        Task<List<SummaryAssetContract>> AssetSummaryList();
                
        /// <summary>
        /// Returns list of opened positions
        /// </summary>
        [Get("/api/trade/openPositions/")]
        Task<List<OrderContract>> OpenPositions();

        /// <summary>
        /// Returns list of opened positions by Volume
        /// </summary>
        /// <param name="volume">Target volume</param>
        /// <returns></returns>
        [Get("/api/trade/openPositions/byVolume/{volume}")]
        Task<List<OrderContract>> OpenPositionsByVolume(decimal volume);

        /// <summary>
        /// Returns list of opened positions by date interval
        /// </summary>
        /// <param name="from">interval start</param>
        /// <param name="to">interval finish</param>
        /// <returns></returns>
        [Get("/api/trade/openPositions/byDate/")]
        Task<List<OrderContract>> OpenPositionsByDate([Query] DateTime from, [Query] DateTime to);

        /// <summary>
        /// Returns list of opened positions by client
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <returns></returns>
        [Get("/api/trade/openPositions/byClient/{clientId}")]
        Task<List<OrderContract>> OpenPositionsByClient(string clientId);

        /// <summary>
        /// Returns list of pending orders 
        /// </summary>
        [Get("/api/trade/pendingOrders/")]
        Task<List<OrderContract>> PendingOrders();

        /// <summary>
        /// Returns list of pending orders by volume
        /// </summary>
        /// <param name="volume">Target volume</param>
        /// <returns></returns>
        [Get("/api/trade/pendingOrders/byVolume/{volume}")]
        Task<List<OrderContract>> PendingOrdersByVolume(decimal volume);

        /// <summary>
        /// Returns list of pending orders by date interval
        /// </summary>
        /// <param name="from">interval start</param>
        /// <param name="to">interval finish</param>
        /// <returns></returns>
        [Get("/api/trade/pendingOrders/byDate/")]
        Task<List<OrderContract>> PendingOrdersByDate([Query] DateTime from, [Query] DateTime to);

        /// <summary>
        /// Returns list of pending orders by client
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <returns></returns>
        [Get("/api/trade/pendingOrders/byClient/{clientId}")]
        Task<List<OrderContract>> PendingOrdersByClient(string clientId);

        /// <summary>
        /// Returns list of orderbooks
        /// </summary>
        /// <param name="clientId">Client Id</param>
        /// <returns></returns>
        [Get("/api/trade/orderbooks/byInstrument/{instrument}")]
        Task<List<OrderBookContract>> OrderBooksByInstrument(string instrument);
    }
}
