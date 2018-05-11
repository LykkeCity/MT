using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Snow.Positions;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Extensions;
using MarginTrading.DataReader.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers.Snow
{
    /// <summary>
    /// Gets info about open positions
    /// </summary>
    [Authorize]
    [Route("api/positions/open")]
    public class OpenPositionsController : Controller, IOpenPositionsApi
    {
        private readonly IOrdersSnapshotReaderService _ordersSnapshotReaderService;

        public OpenPositionsController(IOrdersSnapshotReaderService ordersSnapshotReaderService)
        {
            _ordersSnapshotReaderService = ordersSnapshotReaderService;
        }

        /// <summary>
        /// Get a position by id
        /// </summary>
        [HttpGet, Route("{positionId}")]
        public async Task<OpenPositionContract> Get(string positionId)
        {
            var order = (await _ordersSnapshotReaderService.GetActiveAsync()).FirstOrDefault(o => o.Id == positionId);
            if (order == null)
                return null;

            return Convert(order);
        }

        /// <summary>
        /// Get open positions 
        /// </summary>
        [HttpGet, Route("")]
        public async Task<List<OpenPositionContract>> List(string accountId, string assetPairId)
        {
            IEnumerable<Order> orders = await _ordersSnapshotReaderService.GetActiveAsync();
            if (!string.IsNullOrWhiteSpace(accountId))
                orders = orders.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                orders = orders.Where(o => o.Instrument == assetPairId);

            return orders.Select(Convert).ToList();
        }

        private OpenPositionContract Convert(Order order)
        {
            return new OpenPositionContract
            {
                AccountId = order.AccountId,
                AssetPairId = order.Instrument,
                CurrentVolume = order.Volume,
                Direction = Convert(order.GetOrderType()),
                Id = order.Id,
                OpenPrice = order.OpenPrice,
                PnL = order.FplData.Fpl,
                RelatedOrders = new List<string>(),
                OpenTimestamp = order.OpenDate.RequiredNotNull(nameof(order.OpenDate)),
                TradeId = order.Id + '_' + order.GetOrderType(),
            };
        }

        private PositionDirectionContract Convert(OrderDirection orderOpenType)
        {
            switch (orderOpenType)
            {
                case OrderDirection.Buy:
                    return PositionDirectionContract.Long;
                case OrderDirection.Sell:
                    return PositionDirectionContract.Short;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderOpenType), orderOpenType, null);
            }
        }
    }
}