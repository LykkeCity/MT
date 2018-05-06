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
        
        
        #region Init data

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

        #endregion
      
        
        
        #region State
        
//        [Route("order.list")]
//        [HttpPost]
//        public OrderBackendContract[] GetOpenPositions([FromBody]ClientIdBackendRequest request)
//        {
//            var accountIds = _accountsCacheService.GetAll(request.ClientId).Select(item => item.Id).ToArray();
//
//            var positions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountIds).Select(item => item.ToBackendContract()).ToList();
//            var orders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(accountIds).Select(item => item.ToBackendContract()).ToList();
//
//            positions.AddRange(orders);
//            var result = positions.ToArray();
//
//            return result;
//        }
//
//        [Route("order.account.list")]
//        [HttpPost]
//        public OrderBackendContract[] GetAccountOpenPositions([FromBody]AccountClientIdBackendRequest request)
//        {
//            var account = _accountsCacheService.Get(request.ClientId, request.AccountId);
//
//            var positions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(account.Id).Select(item => item.ToBackendContract()).ToList();
//            var orders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(account.Id).Select(item => item.ToBackendContract()).ToList();
//
//            positions.AddRange(orders);
//            var result = positions.ToArray();
//
//            return result;
//        }
//
//        [Route("order.positions")]
//        [HttpPost]
//        public ClientOrdersBackendResponse GetClientOrders([FromBody]ClientIdBackendRequest request)
//        {
//            var accountIds = _accountsCacheService.GetAll(request.ClientId).Select(item => item.Id).ToArray();
//
//            var positions = _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountIds).ToList();
//            var orders = _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(accountIds).ToList();
//
//            var result = BackendContractFactory.CreateClientOrdersBackendResponse(positions, orders);
//
//            return result;
//        }
        
        #endregion

    }
}
