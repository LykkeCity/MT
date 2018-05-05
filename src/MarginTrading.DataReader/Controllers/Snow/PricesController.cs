using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Snow.Prices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers.Snow
{
    /// <summary>                                                                                       
    /// Provides data about prices
    /// </summary>
    [Authorize]
    [Route("api/prices")]
    public class PricesController : Controller, IPricesApi
    {
        /// <summary>
        /// Get current best prices
        /// </summary>
        /// <remarks>
        /// Post because the query string will be too long otherwise
        /// </remarks>
        [HttpGet, Route("best")]
        public Task<List<BestPriceContract>> Best(string[] assetPairsIds)
        {
            throw new System.NotImplementedException();
        }
    }
}