using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Snow.Positions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers.Snow
{
    /// <summary>
    /// Gets info about positions
    /// </summary>
    [Authorize]
    [Route("api/positions")]
    public class PositionsController : Controller, IPositionsApi
    {
        /// <summary>
        /// Get a position by id
        /// </summary>
        [HttpGet, Route("{positionId}")]
        public Task<OpenPositionContract> Get(string positionId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get open positions 
        /// </summary>
        [HttpGet, Route("open")]
        public Task<List<OpenPositionContract>> ListOpen(string accountId, string instrument)
        {
            throw new System.NotImplementedException();
        }
    }
}