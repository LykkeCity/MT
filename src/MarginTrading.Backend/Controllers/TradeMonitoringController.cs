using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.AssetPairSettings;
using MarginTrading.Backend.Contracts.TradeMonitoring;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Helpers;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.MatchedOrders;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Common.Extensions;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/trade/")]
    [Obsolete]
    public class TradeMonitoringController : Controller, ITradeMonitoringReadingApi
    {
        private readonly IConvertService _convertService;
        private readonly OrdersCache _ordersCache;
        private readonly IMarginTradingBlobRepository _blobRepository;
        private readonly OrderBookList _orderBookList;
        private readonly IContextFactory _contextFactory;

        public TradeMonitoringController(IConvertService convertService, OrdersCache ordersCache,
            IMarginTradingBlobRepository blobRepository, OrderBookList orderBookList, IContextFactory contextFactory)
        {
            _convertService = convertService;
            _ordersCache = ordersCache;
            _blobRepository = blobRepository;
            _orderBookList = orderBookList;
            _contextFactory = contextFactory;
        }

        /// <summary>
        ///     Returns summary asset info from a snapshot (blob)
        /// </summary>
        /// <remarks>
        ///     <p>VolumeLong is a sum of long positions volume</p>
        ///     <p>VolumeShort is a sum of short positions volume</p>
        ///     <p>PnL is a sum of all positions PnL</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns summary info by assets</response>
        [HttpGet]
        [Route("assets/summary")]
        public async Task<List<SummaryAssetContract>> AssetSummaryList()
        {
            var result = new List<SummaryAssetContract>();
            var orders = _ordersCache.GetAll();

            foreach (var order in orders)
            {
                var assetInfo = result.FirstOrDefault(item => item.AssetPairId == order.Instrument);

                if (assetInfo == null)
                {
                    result.Add(new SummaryAssetContract
                    {
                        AssetPairId = order.Instrument,
                        PnL = order.FplData.Fpl,
                        VolumeLong = order.GetOrderType() == OrderDirection.Buy ? order.GetMatchedVolume() : 0,
                        VolumeShort = order.GetOrderType() == OrderDirection.Sell ? order.GetMatchedVolume() : 0
                    });
                }
                else
                {
                    assetInfo.PnL += order.FplData.Fpl;

                    if (order.GetOrderType() == OrderDirection.Buy)
                    {
                        assetInfo.VolumeLong += order.GetMatchedVolume();
                    }
                    else
                    {
                        assetInfo.VolumeShort += order.GetMatchedVolume();
                    }
                }
            }

            return result;
        }

        /// <summary>
        ///     Returns list of opened positions from a snapshot (blob)
        /// </summary>
        /// <remarks>
        ///     <p>Returns list of opened positions with matched volume greater or equal provided "volume" parameter</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns opened positions</response>
        [HttpGet]
        [Route("openPositions/byVolume/{volume}")]
        public async Task<List<DetailedOrderContract>> OpenPositionsByVolume([FromRoute] decimal volume)
        {
            return _ordersCache.GetActive().Where(order => order.GetMatchedVolume() >= volume).Select(Convert).ToList();
        }

        /// <summary>
        ///     Returns list of opened positions from a snapshot (blob)
        /// </summary>
        /// <remarks>
        ///     <p>Returns list of all opened positions</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns opened positions</response>
        [HttpGet]
        [Route("openPositions")]
        public Task<List<DetailedOrderContract>> OpenPositions()
        {
            return OpenPositionsByVolume(0);
        }

        /// <summary>
        ///     Returns list of opened positions from a snapshot (blob) filtered by a date interval
        /// </summary>
        /// <remarks>
        ///     <p>Returns list of date filtered opened positions</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns opened positions</response>
        [HttpGet]
        [Route("openPositions/byDate")]
        public async Task<List<DetailedOrderContract>> OpenPositionsByDate([FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            return _ordersCache.GetActive().Where(order => order.OpenDate >= from.Date && order.OpenDate < to.Date)
                .Select(Convert).ToList();
        }

        /// <summary>
        ///     Returns list of opened positions from a snapshot (blob) filtered by accounts
        /// </summary>
        /// <remarks>
        ///     <p>Returns list of client filtered opened positions</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns opened positions</response>
        [HttpGet]
        [Route("openPositions/byAccounts")]
        public async Task<List<DetailedOrderContract>> OpenPositionsByClient([FromQuery] string[] accountIds)
        {
            return _ordersCache.GetActive().Where(order => accountIds.Contains(order.AccountId)).Select(Convert)
                .ToList();
        }

        /// <summary>
        ///     Returns list of pending orders from a snapshot (blob)
        /// </summary>
        /// <remarks>
        ///     <p>Returns list of pending orders with volume greater or equal provided "volume" parameter</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns pending orders</response>
        [HttpGet]
        [Route("pendingOrders/byVolume/{volume}")]
        public async Task<List<DetailedOrderContract>> PendingOrdersByVolume([FromRoute] decimal volume)
        {
            return _ordersCache.GetPending().Where(order => Math.Abs(order.Volume) >= volume).Select(Convert).ToList();
        }

        /// <summary>
        ///     Returns list of pending orders from a snapshot (blob)
        /// </summary>
        /// <remarks>
        ///     <p>Returns list of all pending orders</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns pending orders</response>
        [HttpGet]
        [Route("pendingOrders")]
        public Task<List<DetailedOrderContract>> PendingOrders()
        {
            return PendingOrdersByVolume(0);
        }

        /// <summary>
        ///     Returns list of pending orders from a snapshot (blob) filtered by a date interval
        /// </summary>
        /// <remarks>
        ///     <p>Returns list of date filtered pending orders</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns pending orders</response>
        [HttpGet]
        [Route("pendingOrders/byDate")]
        public async Task<List<DetailedOrderContract>> PendingOrdersByDate([FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            return _ordersCache.GetPending().Where(order => order.CreateDate >= from && order.CreateDate < to)
                .Select(Convert).ToList();
        }

        /// <summary>
        ///     Returns list of pending orders from a snapshot (blob) filtered by accounts
        /// </summary>
        /// <remarks>
        ///     <p>Returns list of client filtered pending orders</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns pending orders</response>
        [HttpGet]
        [Route("pendingOrders/byAccounts")]
        public async Task<List<DetailedOrderContract>> PendingOrdersByClient([FromQuery] string[] accountIds)
        {
            return _ordersCache.GetPending().Where(order => accountIds.Contains(order.AccountId)).Select(Convert)
                .ToList();
        }


        /// <summary>
        ///     Returns list of orderbooks from a snapshot (blob)
        /// </summary>
        /// <remarks>
        ///     Header "api-key" is required
        /// </remarks>
        /// <response code="200">Returns orderbooks</response>
        [HttpGet]
        [Route("orderbooks/byInstrument/{instrument}")]
        public async Task<List<OrderBookContract>> OrderBooksByInstrument(string instrument)
        {
            // todo dangerous
            Dictionary<string, OrderBook> orderbookState;
            using (_contextFactory.GetReadSyncContext(
                $"{nameof(TradeMonitoringController)}.{nameof(OrderBooksByInstrument)}"))
                orderbookState = _orderBookList.GetOrderBookState();

            var orderbook = orderbookState.GetValueOrDefault(instrument, k => new OrderBook(instrument));
            return new List<OrderBookContract> {Convert(orderbook)};
        }

        private OrderBookContract Convert(OrderBook domainContract)
        {
            return _convertService.Convert<OrderBook, OrderBookContract>(domainContract);
        }

        public static DetailedOrderContract Convert(Order src)
        {
            MatchedOrderContract MatchedOrderToBackendContract(MatchedOrder o) =>
                new MatchedOrderContract
                {
                    OrderId = o.OrderId,
                    LimitOrderLeftToMatch = o.LimitOrderLeftToMatch,
                    Volume = o.Volume,
                    Price = o.Price,
                    MatchedDate = o.MatchedDate
                };

            return new DetailedOrderContract
            {
                Id = src.Id,
                Code = src.Code,
                AccountId = src.AccountId,
                AccountAssetId = src.AccountAssetId,
                Instrument = src.Instrument,
                Status = src.Status.ToType<OrderStatusContract>(),
                CreateDate = src.CreateDate,
                OpenDate = src.OpenDate,
                CloseDate = src.CloseDate,
                ExpectedOpenPrice = src.ExpectedOpenPrice,
                OpenPrice = src.OpenPrice,
                ClosePrice = src.ClosePrice,
                Type = src.GetOrderType().ToType<OrderDirectionContract>(),
                Volume = src.Volume,
                MatchedVolume = src.GetMatchedVolume(),
                MatchedCloseVolume = src.GetMatchedCloseVolume(),
                TakeProfit = src.TakeProfit,
                StopLoss = src.StopLoss,
                Fpl = src.FplData.Fpl,
                PnL = src.FplData.TotalFplSnapshot,
                CloseReason = src.CloseReason.ToType<OrderCloseReasonContract>(),
                RejectReason = src.RejectReason.ToType<OrderRejectReasonContract>(),
                RejectReasonText = src.RejectReasonText,
                CommissionLot = src.CommissionLot,
                OpenCommission = src.GetOpenCommission(),
                CloseCommission = src.GetCloseCommission(),
                SwapCommission = src.SwapCommission,
                MatchedOrders = src.MatchedOrders.Select(MatchedOrderToBackendContract).ToList(),
                MatchedCloseOrders = src.MatchedCloseOrders.Select(MatchedOrderToBackendContract).ToList(),
                OpenExternalOrderId = src.OpenExternalOrderId,
                OpenExternalProviderId = src.OpenExternalProviderId,
                CloseExternalOrderId = src.CloseExternalOrderId,
                CloseExternalProviderId = src.CloseExternalProviderId,
                MatchingEngineMode = src.MatchingEngineMode.ToType<MatchingEngineModeContract>(),
                LegalEntity = src.LegalEntity,
            };
        }
    }
}