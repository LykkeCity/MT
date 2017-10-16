using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Common.BackendContracts;
using MarginTrading.Core;
using MarginTrading.DataReader.Helpers;
using MarginTrading.DataReader.Models;
using MarginTrading.DataReader.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/trade/")]
    public class TradeMonitoringController : Controller
    {
        private readonly IOrdersSnapshotReaderService _ordersSnapshotReaderService;
        private readonly IOrderBookSnapshotReaderService _orderBookSnapshotReaderService;

        public TradeMonitoringController(IOrdersSnapshotReaderService ordersSnapshotReaderService,
            IOrderBookSnapshotReaderService orderBookSnapshotReaderService)
        {
            _ordersSnapshotReaderService = ordersSnapshotReaderService;
            _orderBookSnapshotReaderService = orderBookSnapshotReaderService;
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
        public async Task<List<SummaryAssetInfo>> GetAssetsSummary()
        {
            var result = new List<SummaryAssetInfo>();
            var orders = await _ordersSnapshotReaderService.GetAllAsync();

            foreach (var order in orders)
            {
                var assetInfo = result.FirstOrDefault(item => item.AssetPairId == order.Instrument);

                if (assetInfo == null)
                {
                    result.Add(new SummaryAssetInfo
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
        public async Task<List<OrderContract>> GetOpenPositionsByVolume([FromRoute]decimal volume)
        {
            return (await _ordersSnapshotReaderService.GetActiveAsync())
                .Where(order => order.GetMatchedVolume() >= volume).Select(OrderExtensions.ToBaseContract)
                .ToList();
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
        public Task<List<OrderContract>> GetAllOpenPositions()
        {
            return GetOpenPositionsByVolume(0);
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
        public async Task<List<OrderContract>> GetOpenPositionsByDate([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            return (await _ordersSnapshotReaderService.GetActiveAsync())
                .Where(order => order.OpenDate >= from.Date && order.OpenDate< to.Date)
                .Select(OrderExtensions.ToBaseContract)
                .ToList();
        }

        /// <summary>
        ///     Returns list of opened positions from a snapshot (blob) filtered by client
        /// </summary>
        /// <remarks>
        ///     <p>Returns list of client filtered opened positions</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns opened positions</response>
        [HttpGet]
        [Route("openPositions/byClient/{clientId}")]
        public async Task<List<OrderContract>> GetOpenPositionsByClient([FromRoute]string clientId)
        {
            return (await _ordersSnapshotReaderService.GetActiveAsync())
                .Where(order => order.ClientId == clientId)
                .Select(OrderExtensions.ToBaseContract)
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
        public async Task<List<OrderContract>> GetPendingOrdersByVolume([FromRoute]decimal volume)
        {
            return (await _ordersSnapshotReaderService.GetPendingAsync())
                .Where(order => Math.Abs(order.Volume) >= volume).Select(OrderExtensions.ToBaseContract)
                .ToList();
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
        public Task<List<OrderContract>> GetAllPendingOrders()
        {
            return GetPendingOrdersByVolume(0);
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
        public async Task<List<OrderContract>> GetPendingOrdersByDate([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            return (await _ordersSnapshotReaderService.GetPendingAsync())
                .Where(order => order.CreateDate >= from && order.CreateDate < to)
                .Select(OrderExtensions.ToBaseContract)
                .ToList();
        }

        /// <summary>
        ///     Returns list of pending orders from a snapshot (blob) filtered by client
        /// </summary>
        /// <remarks>
        ///     <p>Returns list of client filtered pending orders</p>
        ///     <p>Header "api-key" is required</p>
        /// </remarks>
        /// <response code="200">Returns pending orders</response>
        [HttpGet]
        [Route("pendingOrders/byClient/{clientId}")]
        public async Task<List<OrderContract>> GetPendingOrdersByClient([FromRoute]string clientId)
        {
            return (await _ordersSnapshotReaderService.GetPendingAsync())
                .Where(order => order.ClientId == clientId)
                .Select(OrderExtensions.ToBaseContract)
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
        public async Task<List<OrderBookModel>> GetOrderBooks(string instrument)
        {
            var orderbooks = await _orderBookSnapshotReaderService.GetAllLimitOrders(instrument);
            return new List<OrderBookModel>
            {
                new OrderBookModel
                {
                    Instrument = instrument,
                    Buy = orderbooks.Buy.ToList(),
                    Sell = orderbooks.Sell.ToList()
                }
            };
        }
    }
}