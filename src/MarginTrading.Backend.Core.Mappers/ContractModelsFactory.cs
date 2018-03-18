using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.Orderbooks;
using MarginTrading.Backend.Core.TradingConditions;
using MarginTrading.Contract.BackendContracts;

namespace MarginTrading.Backend.Core.Mappers
{
    public class BackendContractFactory
    {
        
        public static AccountHistoryBackendResponse CreateAccountHistoryBackendResponse(IEnumerable<IMarginTradingAccountHistory> accounts, IEnumerable<Order> openPositions, IEnumerable<IOrderHistory> historyOrders)
        {
            return new AccountHistoryBackendResponse
            {
                Account = accounts.Select(item => item.ToBackendContract()).ToArray(),
                OpenPositions = openPositions.Select(item => item.ToBackendHistoryContract())
                    .OrderByDescending(item => item.OpenDate).ToArray(),
                PositionsHistory = historyOrders.Where(item => item.OpenDate.HasValue && item.CloseDate.HasValue)
                    .Select(item => item.ToBackendHistoryContract()).OrderByDescending(item => item.OpenDate).ToArray()
            };
        }
        
        public static AccountNewHistoryBackendResponse CreateAccountNewHistoryBackendResponse(IEnumerable<IMarginTradingAccountHistory> accounts, IEnumerable<IOrder> openOrders, IEnumerable<IOrderHistory> historyOrders)
        {
            var items = new List<AccountHistoryItemBackend>();
            var history = historyOrders.Where(item => item.OpenDate.HasValue && item.CloseDate.HasValue).ToList();

            items.AddRange(accounts.Select(item => new AccountHistoryItemBackend { Account = item.ToBackendContract(), Date = item.Date }).ToList());

            items.AddRange(openOrders.Select(item => new AccountHistoryItemBackend { Position = item.ToBackendHistoryContract(), Date = item.OpenDate.Value }).ToList());

            items.AddRange(history.Select(item =>
                new AccountHistoryItemBackend
                {
                    Position = item.ToBackendHistoryOpenedContract(),
                    Date = item.OpenDate.Value
                }).ToList());
            
            items.AddRange(history.Select(item =>
                    new AccountHistoryItemBackend
 {
                        Position = item.ToBackendHistoryContract(),
                        Date = item.CloseDate.Value
                    })
                .ToList());

            items = items.OrderByDescending(item => item.Date).ToList();

            return new AccountNewHistoryBackendResponse
            {
                HistoryItems = items.ToArray()
            };
        }
        
        public static ClientOrdersBackendResponse CreateClientOrdersBackendResponse(IEnumerable<IOrder> positions, IEnumerable<IOrder> orders)
        {
            return new ClientOrdersBackendResponse
            {
                Positions = positions.Select(item => item.ToBackendContract()).ToArray(),
                Orders = orders.Select(item => item.ToBackendContract()).ToArray()
            };
        }
        
        public static InitAccountInstrumentsBackendResponse CreateInitAccountInstrumentsBackendResponse(Dictionary<string, IAccountAssetPair[]> accountAssets)
        {
            return new InitAccountInstrumentsBackendResponse
            {
                AccountAssets = accountAssets.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToBackendContract()).ToArray())
            };
        }
        
        public static InitChartDataBackendResponse CreateInitChartDataBackendResponse(Dictionary<string, List<GraphBidAskPair>> chartData)
        {
            return new InitChartDataBackendResponse
            {
                ChartData = chartData.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToBackendContract()).ToArray())
            };
        }
        
        public static InitDataBackendResponse CreateInitDataBackendResponse(IEnumerable<IMarginTradingAccount> accounts,
            Dictionary<string, IAccountAssetPair[]> accountAssetPairs, bool isLive)
        {
            return new InitDataBackendResponse
            {
                Accounts = accounts.Select(item => item.ToFullBackendContract(isLive)).ToArray(),
                AccountAssetPairs = accountAssetPairs.ToDictionary(pair => pair.Key, pair => pair.Value.Select(item => item.ToBackendContract()).ToArray()),
            };
        }
        
        public static OpenOrderBackendResponse CreateOpenOrderBackendResponse(IOrder order)
        {
            return new OpenOrderBackendResponse
            {
                Order = order.ToBackendContract()
            };
        }
        
        public static OrderbooksBackendResponse CreateOrderbooksBackendResponse(OrderBook orderbook)
        {
            return new OrderbooksBackendResponse
            {
                Orderbook = orderbook.ToBackendContract()
            };
        }
    }
}