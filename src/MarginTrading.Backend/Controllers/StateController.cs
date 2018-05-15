using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Attributes;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Services;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/state")]
    public class StateController : Controller
    {
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        private readonly IQuoteCacheService _quoteCacheService;

        public StateController(
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IQuoteCacheService quoteCacheService)
        {
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _quoteCacheService = quoteCacheService;
        }
        
        [Route("prices")]
        [HttpPost]
        [SkipMarginTradingEnabledCheck]
        public Dictionary<string, InstrumentBidAskPairContract> GetBestPrices([FromBody]InitPricesBackendRequest request)
        {
            IEnumerable<KeyValuePair<string, InstrumentBidAskPair>> allQuotes = _quoteCacheService.GetAllQuotes();

            if (request.AssetIds != null && request.AssetIds.Any())
            {
                allQuotes = allQuotes.Where(q => request.AssetIds.Contains(q.Key));
            }

            return allQuotes.ToDictionary(q => q.Key, q => q.Value.ToBackendContract());
        }

        
        [Route("orders/{accountId}")]
        [HttpGet]
        public List<OrderBackendContract> GetOrders(string accountId)
        {
            var account = _accountsCacheService.Get(accountId);
            
            var orders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(account.Id)
                .Select(item => item.ToBackendContract()).ToList();

            return orders;
        }

        [Route("positions/{accountId}")]
        [HttpPost]
        public List<OrderBackendContract>  GetOpenPositions(string accountId)
        {
            var account = _accountsCacheService.Get(accountId);

            var positions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(account.Id)
                .Select(item => item.ToBackendContract()).ToList();

            return positions;
        }
        
    }
}
