using System;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services;
using MarginTrading.Common.Middleware;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/service")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class ServiceController : Controller
    {
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly OrdersCache _ordersCache;

        public ServiceController(
            IQuoteCacheService quoteCacheService,
            OrdersCache ordersCache)
        {
            _quoteCacheService = quoteCacheService;
            _ordersCache = ordersCache;
        }

        [HttpDelete]
        [Route("bestprice/{assetPair}")]
        public MtBackendResponse<bool> ClearBestBriceCache(string assetPair)
        {
            var positions = _ordersCache.Positions.GetPositionsByInstrument(assetPair).ToList();
            if (positions.Any())
            {
                return MtBackendResponse<bool>.Error(
                    $"Cannot delete [{assetPair}] best price because there are {positions.Count} opened positions.");
            }
            
            var orders = _ordersCache.Active.GetOrdersByInstrument(assetPair).ToList();
            if (orders.Any())
            {
                return MtBackendResponse<bool>.Error(
                    $"Cannot delete [{assetPair}] best price because there are {orders.Count} active orders.");
            }
            
            _quoteCacheService.RemoveQuote(assetPair);
            
            return MtBackendResponse<bool>.Ok(true);
        }
    }
}