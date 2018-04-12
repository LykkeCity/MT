using System;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    public class AccountUpdateService : IAccountUpdateService
    {
        private readonly IFplService _fplService;
        private readonly IAccountGroupCacheService _accountGroupCacheService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetsCache _assetsCache;

        public AccountUpdateService(
            IFplService fplService,
            IAccountGroupCacheService accountGroupCacheService,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IAssetsCache assetsCache)
        {
            _fplService = fplService;
            _accountGroupCacheService = accountGroupCacheService;
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _assetsCache = assetsCache;
        }

        public void UpdateAccount(IMarginTradingAccount account, AccountFpl accountFpl, Order[] orders = null)
        {
            if (orders == null)
            {
                orders = _ordersCache.ActiveOrders.GetOrdersByAccountIds(account.Id).ToArray();
            }

            var accuracy = _assetsCache.GetAssetAccuracy(account.BaseAssetId);
            
            accountFpl.PnL = Math.Round(orders.Sum(x => x.GetTotalFpl()), accuracy);

            accountFpl.UsedMargin = Math.Round(orders.Sum(item => item.GetMarginMaintenance()),
                accuracy);
            accountFpl.MarginInit = Math.Round(orders.Sum(item => item.GetMarginInit()),
                accuracy);
            accountFpl.OpenPositionsCount = orders.Length;

            var accountGroup = _accountGroupCacheService.GetAccountGroup(account.TradingConditionId, account.BaseAssetId);

            if (accountGroup == null)
            {
                throw new Exception(string.Format(MtMessages.AccountGroupForTradingConditionNotFound, account.TradingConditionId, account.BaseAssetId));
            }

            accountFpl.MarginCallLevel = accountGroup.MarginCall;
            accountFpl.StopoutLevel = accountGroup.StopOut;
            accountFpl.CalculatedHash = accountFpl.ActualHash;
        }

        public bool IsEnoughBalance(Order order)
        {
            _fplService.CalculateMargin(order, order.FplData);
            var orderMargin = order.GetMarginInit();
            var accountMarginAvailable = _accountsCacheService.Get(order.ClientId, order.AccountId).GetMarginAvailable(); 
            
            return accountMarginAvailable >= orderMargin;
        }

        public MarginTradingAccount GuessAccountWithOrder(Order order)
        {
            var accountFpl = new AccountFpl();
            var newInstance = MarginTradingAccount.Create(_accountsCacheService.Get(order.ClientId, order.AccountId), accountFpl);

            var orders = _ordersCache.ActiveOrders.GetOrdersByInstrumentAndAccount(order.Instrument, order.AccountId).ToList();
            orders.Add(order);

            UpdateAccount(newInstance, accountFpl, orders.ToArray());

            return newInstance;
        }
    }
}
