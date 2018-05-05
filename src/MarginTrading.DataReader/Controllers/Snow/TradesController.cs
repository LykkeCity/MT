using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Snow.Orders;
using MarginTrading.Backend.Contracts.Snow.Trades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers.Snow
{
    /// <summary>
    /// Provides data about trades
    /// </summary>
    [Authorize]
    [Route("api/trades")]
    public class TradesController : Controller, ITradesApi
    {
        /// <summary>
        /// Get a trade by id 
        /// </summary>
        [HttpGet, Route("{tradeId}")]
        public Task<TradeContract> Get(string tradeId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get trades with optional filtering by order or position 
        /// </summary>
        [HttpGet, Route("")]
        public Task<List<TradeContract>> List(string orderId, string positionId)
        {
            throw new System.NotImplementedException();
        }
    }
}