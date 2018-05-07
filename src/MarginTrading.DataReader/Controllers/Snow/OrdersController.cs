using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AzureRepositories.Snow.Trades;
using MarginTrading.Backend.Contracts.Snow.Orders;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Extensions;
using MarginTrading.DataReader.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers.Snow
{
    /// <summary>
    /// Provides data about active orders
    /// </summary>
    [Authorize]
    [Route("api/orders")]
    public class OrdersController : Controller, IOrdersApi
    {
        private readonly ITradesRepository _tradesRepository;
        private readonly IOrdersSnapshotReaderService _ordersSnapshotReaderService;
        private readonly IOrdersHistoryRepository _ordersHistoryRepository;

        public OrdersController(ITradesRepository tradesRepository,
            IOrdersSnapshotReaderService ordersSnapshotReaderService, IOrdersHistoryRepository ordersHistoryRepository)
        {
            _tradesRepository = tradesRepository;
            _ordersSnapshotReaderService = ordersSnapshotReaderService;
            _ordersHistoryRepository = ordersHistoryRepository;
        }

        /// <summary>
        /// Get order by id 
        /// </summary>
        [HttpGet, Route("{orderId}")]
        public async Task<OrderContract> Get(string orderId)
        {
            var order = (await _ordersSnapshotReaderService.GetAllAsync()).FirstOrDefault(o => o.Id == orderId);
            if (order != null)
            {
                return Convert(order);
            }

            var trade = await _tradesRepository.GetAsync(orderId);
            if (trade == null)
            {
                return null;
            }

            var history = await _ordersHistoryRepository.GetHistoryAsync(new[] {trade.AccountId},
                trade.TradeTimestamp - TimeSpan.FromSeconds(1), null);

            if (!history.Any())
            {
                return null;
            }

            var lastHistoryRecord = history.OrderByDescending(h => h.UpdateTimestamp).First();
            return Convert(lastHistoryRecord);
        }

        /// <summary>
        /// Get orders by parent order id
        /// </summary>
        [HttpGet, Route("by-parent/{parentOrderId}")]
        public Task<List<OrderContract>> ListByParent(string parentOrderId)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Get open orders with optional filtering
        /// </summary>
        [HttpGet, Route("open")]
        public Task<List<OrderContract>> ListOpen(string accountId, string instrument)
        {
            throw new System.NotImplementedException();
        }


        //todo: move to history
        /// <summary>
        /// Get executed orders with optional filtering
        /// </summary>
        [HttpGet, Route("executed")]
        public Task<List<OrderContract>> ListExecuted(string accountId, string instrument)
        {
            throw new System.NotImplementedException();
        }

        private OrderContract Convert(IOrderHistory lastHistoryRecord)
        {
            return new OrderContract
            {
                AccountId = lastHistoryRecord.AccountId,
                AssetPairId = lastHistoryRecord.Instrument,
                CreatedTimestamp = lastHistoryRecord.CreateDate,
                Direction = lastHistoryRecord.Type.ToType<OrderDirectionContract>(),
                ExecutionPrice = lastHistoryRecord.OpenPrice
            };
        }

        private OrderContract Convert(Order order)
        {
            throw new System.NotImplementedException();
        }
    }
}