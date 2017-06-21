using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Core;
using MarginTrading.Core.Messages;
using MarginTradingHelpers = MarginTrading.Services.Helpers.MarginTradingHelpers;

namespace MarginTrading.Services
{
    public interface IOrderReader
    {
        IEnumerable<Order> GetOrders(params string[] accountIds);
        IEnumerable<Order> GetAll();
        Order GetOrderById(string orderId);
        IEnumerable<Order> GetActive();
        IEnumerable<Order> GetPending();
    }

    public class OrdersCache : IOrderReader
    {
        public OrderCacheGroup ActiveOrders { get; private set; }
        public OrderCacheGroup WaitingForExecutionOrders { get; private set; }
        public OrderCacheGroup ClosingOrders { get; private set; }

        public IEnumerable<Order> GetOrders(params string[] accountIds)
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                return ActiveOrders.GetOrdersByAccountIds(accountIds)
                        .Union(WaitingForExecutionOrders.GetOrdersByAccountIds(accountIds));
            }
        }

        public IEnumerable<Order> GetAll()
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
                return ActiveOrders.GetAllOrders()
                        .Union(WaitingForExecutionOrders.GetAllOrders())
                        .Union(ClosingOrders.GetAllOrders());
        }

        public IEnumerable<Order> GetActive()
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
                return ActiveOrders.GetAllOrders();
        }

        public IEnumerable<Order> GetPending()
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
                return ActiveOrders.GetAllOrders();
        }

        public Order GetOrderById(string orderId)
        {
            lock (MarginTradingHelpers.TradingMatchingSync)
            {
                Order result;

                if (WaitingForExecutionOrders.TryGetOrderById(orderId, out result))
                    return result;

                if (ActiveOrders.TryGetOrderById(orderId, out result))
                    return result;

                throw new Exception(string.Format(MtMessages.OrderNotFound, orderId));
            }
        }

        public void InitOrders(List<Order> orders)
        {
            ActiveOrders = new OrderCacheGroup(orders, OrderStatus.Active);
            WaitingForExecutionOrders = new OrderCacheGroup(orders, OrderStatus.WaitingForExecution);
            ClosingOrders = new OrderCacheGroup(orders, OrderStatus.Closing);
        }
    }

    public class OrderCacheManager : TimerPeriod, IDisposable
    {
        private readonly OrdersCache _orderCache;
        private readonly IMarginTradingBlobRepository _marginTradingBlobRepository;
        private readonly ILog _log;
        private const string BlobName= "orders";

        public OrderCacheManager(OrdersCache orderCache,
            IMarginTradingBlobRepository marginTradingBlobRepository,
            ILog log) : base(nameof(OrderCacheManager), 5000, log)
        {
            _orderCache = orderCache;
            _marginTradingBlobRepository = marginTradingBlobRepository;
            _log = log;
        }

        public override void Start()
        {
            var orders = _marginTradingBlobRepository.Read<List<Order>>(LykkeConstants.StateBlobContainer, BlobName) ?? new List<Order>();

            _orderCache.InitOrders(orders);

            base.Start();
        }

        public override async Task Execute()
        {
            await DumpToRepository();
        }

        public void Dispose()
        {
            DumpToRepository().Wait();
        }

        private async Task DumpToRepository()
        {

            try
            {
                var orders = _orderCache.GetAll();

                if (orders != null)
                {
                    await _marginTradingBlobRepository.Write(LykkeConstants.StateBlobContainer, BlobName, orders);
                }
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(OrdersCache), "Save orders", "", ex);
            }
        }
    }
}