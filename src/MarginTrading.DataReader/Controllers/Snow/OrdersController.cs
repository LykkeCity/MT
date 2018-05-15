using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AzureRepositories.Snow.OrdersById;
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
        private const string CloseSiffix = "_close";
        private readonly IOrdersSnapshotReaderService _ordersSnapshotReaderService;
        private readonly IOrdersHistoryRepository _ordersHistoryRepository;
        private readonly IOrdersByIdRepository _ordersByIdRepository;

        public OrdersController(IOrdersSnapshotReaderService ordersSnapshotReaderService,
            IOrdersHistoryRepository ordersHistoryRepository, IOrdersByIdRepository ordersByIdRepository)
        {
            _ordersSnapshotReaderService = ordersSnapshotReaderService;
            _ordersHistoryRepository = ordersHistoryRepository;
            _ordersByIdRepository = ordersByIdRepository;
        }

        /// <summary>
        /// Get order by id 
        /// </summary>
        [HttpGet, Route("{orderId}")]
        public async Task<OrderContract> Get(string orderId)
        {
            var (realOrderId, isCloseOrder) = ParseFakeOrderId(orderId);
            if (!isCloseOrder)
            {
                var order = (await _ordersSnapshotReaderService.GetAllAsync()).FirstOrDefault(o => o.Id == realOrderId);
                if (order != null)
                    return Convert(order);
            }

            var orderById = await _ordersByIdRepository.GetAsync(realOrderId);
            if (orderById == null)
                return null;

            var history = await _ordersHistoryRepository.GetHistoryAsync(new[] {orderById.AccountId},
                orderById.OrderCreatedTime - TimeSpan.FromSeconds(1), null);

            if (!history.Any())
                return null;

            var lastHistoryRecord =
                history.Where(h => h.Id == orderId).OrderByDescending(h => h.UpdateTimestamp).First();

            if (isCloseOrder && lastHistoryRecord.Status != OrderStatus.Closed)
                return null;

            return Convert(lastHistoryRecord, isCloseOrder);
        }

        private (string RealOrderId, bool IsCloseOrder) ParseFakeOrderId(string orderId)
        {
            return orderId.EndsWith(CloseSiffix)
                ? (orderId.Substring(0, orderId.Length - CloseSiffix.Length), true)
                : (orderId, false);
        }

        /// <summary>
        /// Get orders by parent order id
        /// </summary>
        [HttpGet, Route("by-parent-order/{parentOrderId}")]
        public async Task<List<OrderContract>> ListByParentOrder(string parentOrderId)
        {
            return new List<OrderContract>(); // todo
        }

        /// <summary>
        /// Get orders by parent position id
        /// </summary>
        [HttpGet, Route("by-parent-position/{parentPositionId}")]
        public async Task<List<OrderContract>> ListByParentPosition(string parentPositionId)
        {
            var orderById = await _ordersByIdRepository.GetAsync(parentPositionId);
            if (orderById == null)
                return new List<OrderContract>();

            var history = await _ordersHistoryRepository.GetHistoryAsync(new[] {orderById.AccountId},
                orderById.OrderCreatedTime - TimeSpan.FromSeconds(1), null);

            var lastHistoryRecords = history.Where(h => h.ParentPositionId == parentPositionId)
                .OrderByDescending(h => h.UpdateTimestamp).GroupBy(o => o.Id,
                    (id, orders) => orders.OrderByDescending(h => h.UpdateTimestamp).First());

            return lastHistoryRecords.SelectMany(MakeOrderContractsFromHistory).ToList();
        }

        /// <summary>
        /// Get open orders with optional filtering
        /// </summary>
        [HttpGet, Route("open")]
        public async Task<List<OrderContract>> ListOpen(string accountId, string assetPairId)
        {
            IEnumerable<Order> pending = await _ordersSnapshotReaderService.GetPendingAsync();
            if (!string.IsNullOrWhiteSpace(accountId))
                pending = pending.Where(o => o.AccountId == accountId);

            if (!string.IsNullOrWhiteSpace(assetPairId))
                pending = pending.Where(o => o.Instrument == assetPairId);

            return pending.Select(Convert).ToList(); // do not show a pending close order for closing status
        }

        //todo: move to history
        /// <summary>
        /// Get executed orders with optional filtering
        /// </summary>
        [HttpGet, Route("executed")]
        public async Task<List<OrderContract>> ListExecuted(string accountId, string assetPairId)
        {
            var history = !string.IsNullOrWhiteSpace(accountId)
                ? await _ordersHistoryRepository.GetHistoryAsync(new[] {accountId}, null, null)
                : await _ordersHistoryRepository.GetHistoryAsync();

            if (!string.IsNullOrWhiteSpace(assetPairId))
                history = history.Where(o => o.Instrument == assetPairId);

            var pendingIds = (await _ordersSnapshotReaderService.GetPendingAsync()).Select(o => o.Id).ToHashSet();

            return history.Where(h => !pendingIds.Contains(h.Id)).SelectMany(MakeOrderContractsFromHistory).ToList();
        }

        private static OrderContract Convert(IOrderHistory history, bool isCloseOrder)
        {
            var orderDirection = GetOrderDirection(history.Type, isCloseOrder);
            return new OrderContract
            {
                Id = history.Id + (isCloseOrder ? CloseSiffix : ""),
                AccountId = history.AccountId,
                AssetPairId = history.Instrument,
                CreatedTimestamp = history.CreateDate,
                Direction = history.Type.ToType<OrderDirectionContract>(),
                ExecutionPrice = history.OpenPrice,
                ExpectedOpenPrice = history.ExpectedOpenPrice,
                ForceOpen = true,
                ModifiedTimestamp = history.UpdateTimestamp,
                Originator = OriginatorTypeContract.Investor,
                ParentOrderId = history.ParentOrderId,
                PositionId = history.Id,
                RelatedOrders = new List<string>(),
                Status = Convert(history.Status),
                TradesIds = GetTrades(history.Id, history.Status, orderDirection),
                Type = history.OpenPrice == null ? OrderTypeContract.Market : OrderTypeContract.Limit,
                ValidityTime = null,
                Volume = history.Volume,
            };
        }

        private static List<string> GetTrades(string orderId, OrderStatus status, OrderDirection orderDirection)
        {
            if (status == OrderStatus.WaitingForExecution)
                return new List<string>();

            return new List<string> {orderId + '_' + orderDirection};
        }

        private static OrderDirection GetOrderDirection(OrderDirection openDirection, bool isCloseOrder)
        {
            return !isCloseOrder ? openDirection :
                openDirection == OrderDirection.Buy ? OrderDirection.Sell : OrderDirection.Buy;
        }

        private static OrderStatusContract Convert(OrderStatus orderStatus)
        {
            switch (orderStatus)
            {
                case OrderStatus.WaitingForExecution:
                    return OrderStatusContract.Active;
                case OrderStatus.Active:
                    return OrderStatusContract.Executed; // todo: fix
                case OrderStatus.Closed:
                    return OrderStatusContract.Executed;
                case OrderStatus.Rejected:
                    return OrderStatusContract.Rejected;
                case OrderStatus.Closing:
                    return OrderStatusContract.Active;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orderStatus), orderStatus, null);
            }
        }

        private static OrderContract Convert(Order order)
        {
            var orderDirection = GetOrderDirection(order.GetOrderType(), false);
            return new OrderContract
            {
                Id = order.Id,
                AccountId = order.AccountId,
                AssetPairId = order.Instrument,
                CreatedTimestamp = order.CreateDate,
                Direction = order.GetOrderType().ToType<OrderDirectionContract>(),
                ExecutionPrice = order.OpenPrice,
                ExpectedOpenPrice = order.ExpectedOpenPrice,
                ForceOpen = true,
                ModifiedTimestamp = order.OpenDate ?? order.CreateDate,
                Originator = OriginatorTypeContract.Investor,
                ParentOrderId = null,
                PositionId = order.Id,
                RelatedOrders = new List<string>(),
                Status = Convert(order.Status),
                TradesIds = GetTrades(order.Id, order.Status, orderDirection),
                Type = order.OpenPrice == null ? OrderTypeContract.Market : OrderTypeContract.Limit,
                ValidityTime = null,
                Volume = order.Volume,
            };
        }

        private static IEnumerable<OrderContract> MakeOrderContractsFromHistory(IOrderHistory r)
        {
            yield return Convert(r, false);

            if (r.Status == OrderStatus.Closed)
                yield return Convert(r, true);
        }
    }
}