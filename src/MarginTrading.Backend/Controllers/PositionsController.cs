using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.AssetPairs;
using MarginTrading.Backend.Services.TradingConditions;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/positions")]
    public class PositionsController : Controller, IPositionsApi
    {
        private readonly IMarginTradingAccountHistoryRepository _accountsHistoryRepository;
        private readonly IMarginTradingOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IMicrographCacheService _micrographCacheService;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IMarketMakerMatchingEngine _matchingEngine;
        private readonly ITradingEngine _tradingEngine;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly IConsole _consoleWriter;
        private readonly OrdersCache _ordersCache;
        private readonly MarginTradingSettings _marginSettings;
        private readonly AccountManager _accountManager;
        private readonly IAssetPairDayOffService _assetDayOffService;
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly IIdentityGenerator _identityGenerator;

        public PositionsController(
            IMarginTradingAccountHistoryRepository accountsHistoryRepository,
            IMarginTradingOrdersHistoryRepository ordersHistoryRepository,
            IMicrographCacheService micrographCacheService,
            IAccountAssetsCacheService accountAssetsCacheService,
            IAssetPairsCache assetPairsCache,
            IMarketMakerMatchingEngine matchingEngine,
            ITradingEngine tradingEngine,
            IAccountsCacheService accountsCacheService,
            IMarginTradingOperationsLogService operationsLogService,
            IConsole consoleWriter,
            OrdersCache ordersCache,
            MarginTradingSettings marginSettings,
            AccountManager accountManager,
            IAssetPairDayOffService assetDayOffService,
            IQuoteCacheService quoteCacheService,
            IIdentityGenerator identityGenerator)
        {
            _accountsHistoryRepository = accountsHistoryRepository;
            _ordersHistoryRepository = ordersHistoryRepository;
            _micrographCacheService = micrographCacheService;
            _accountAssetsCacheService = accountAssetsCacheService;
            _assetPairsCache = assetPairsCache;
            _matchingEngine = matchingEngine;
            _tradingEngine = tradingEngine;
            _accountsCacheService = accountsCacheService;
            _operationsLogService = operationsLogService;
            _consoleWriter = consoleWriter;
            _ordersCache = ordersCache;
            _marginSettings = marginSettings;
            _accountManager = accountManager;
            _assetDayOffService = assetDayOffService;
            _quoteCacheService = quoteCacheService;
            _identityGenerator = identityGenerator;
        }
        
        /// <summary>
        /// Close opened position
        /// </summary>
        /// <param name="positionId">Id of position</param>
        /// <param name="request">Additional info for close</param>
        [Route("{positionId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        public async Task CloseAsync([FromRoute] string positionId, [FromBody] PositionCloseRequest request)
        {
            if (!_ordersCache.ActiveOrders.TryGetOrderById(positionId, out var order))
            {
                throw new InvalidOperationException("Position not found");
            }

            if (_assetDayOffService.IsDayOff(order.Instrument))
            {
                throw new InvalidOperationException("Trades for instrument are not available");
            }

            var reason =
                request.Originator == OriginatorTypeContract.OnBehalf ||
                request.Originator == OriginatorTypeContract.System
                    ? OrderCloseReason.ClosedByBroker
                    : OrderCloseReason.Close;

            order = await _tradingEngine.CloseActiveOrderAsync(positionId, reason, request.Comment);

            if (order.Status != OrderStatus.Closed && order.Status != OrderStatus.Closing)
            {
                throw new InvalidOperationException(order.CloseRejectReasonText);
            }

            _consoleWriter.WriteLine(
                $"action position.close, orderId = {positionId}");
            _operationsLogService.AddLog("action order.close", order.ClientId, order.AccountId, request.ToJson(),
                order.ToJson());
        }

        /// <summary>
        /// Close group of opened positions by itrument and direction
        /// </summary>
        /// <param name="instrumentId">Positions instrument</param>
        /// <param name="direction">Positions direction (Long or Short), optional</param>
        /// <param name="request">Additional info for close</param>
        [Route("instrument-group/{instrumentId}")]
        [MiddlewareFilter(typeof(RequestLoggingPipeline))]
        [HttpDelete]
        public Task CloseGroupAsync([FromRoute] string instrumentId,
            [FromBody] PositionCloseRequest request, [FromQuery] PositionDirectionContract? direction = null)
        {
            throw new NotImplementedException();
        }
    }
}
