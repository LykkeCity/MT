using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.DataReader.Services;
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
        private readonly IQuotesSnapshotReadersService _quotesSnapshotReadersService;

        public PricesController(IQuotesSnapshotReadersService quotesSnapshotReadersService)
        {
            _quotesSnapshotReadersService = quotesSnapshotReadersService;
        }

        /// <summary>
        /// Get current best prices
        /// </summary>
        /// <remarks>
        /// Post because the query string will be too long otherwise
        /// </remarks>
        [HttpGet, Route("best")]
        public async Task<List<BestPriceContract>> Best(string[] assetPairsIds)
        {
            var dict = _quotesSnapshotReadersService.GetSnapshotAsync();
            return assetPairsIds.Select(pid => dict.GetValueOrDefault(pid)).Where(p => p != null).Select(Convert)
                .ToList();
        }

        private BestPriceContract Convert(InstrumentBidAskPair arg)
        {
            return new BestPriceContract
            {
                Ask = arg.Ask,
                Bid = arg.Bid,
                Id = arg.Instrument,
                Timestamp = arg.Date,
            };
        }
    }
}