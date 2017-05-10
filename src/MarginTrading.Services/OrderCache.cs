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
                        .Union(WaitingForExecutionOrders.GetAllOrders());
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

        public OrderCacheManager(OrdersCache orderCache,
            IMarginTradingBlobRepository marginTradingBlobRepository,
            ILog log) : base(nameof(OrderCacheManager), 1000, log)
        {
            _orderCache = orderCache;
            _marginTradingBlobRepository = marginTradingBlobRepository;
        }

        public override void Start()
        {
            // TODO: Restore from storage on start

            //var blobRepository = ApplicationContainer.Resolve<IMarginTradingBlobRepository>();

            
            //string blobKey = "TE_orders";
            //var orders = blobRepository.Read<List<Order>>(blobContainer, blobKey) ?? new List<Order>();

            //TODO: added for tests, change to restore from storage
            _orderCache.InitOrders(new List<Order>());

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
            // TODO: Implement
        }
    }
}