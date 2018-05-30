using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    [UsedImplicitly]
    public class AccountUpdateService : IAccountUpdateService
    {
        private readonly IFplService _fplService;
        private readonly ITradingConditionsCacheService _tradingConditionsCache;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetsCache _assetsCache;

        public AccountUpdateService(
            IFplService fplService,
            ITradingConditionsCacheService tradingConditionsCache,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IAssetsCache assetsCache)
        {
            _fplService = fplService;
            _tradingConditionsCache = tradingConditionsCache;
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _assetsCache = assetsCache;
        }

        public void UpdateAccount(IMarginTradingAccount account)
        {
            UpdateAccount(account, GetActiveOrders(account.Id), GetPendingOrders(account.Id));
        }

        public bool IsEnoughBalance(Order order)
        {
            _fplService.CalculateMargin(order, order.FplData);
            var orderMargin = order.GetMarginInit();
            var accountMarginAvailable = _accountsCacheService.Get(order.AccountId).GetMarginAvailable(); 
            
            return accountMarginAvailable >= orderMargin;
        }

        public MarginTradingAccount GuessAccountWithNewActiveOrder(Order order)
        {
            var newInstance = MarginTradingAccount.Create(_accountsCacheService.Get(order.AccountId));

            var activeOrders = GetActiveOrders(newInstance.Id);
            activeOrders.Add(order);
            
            var pendingOrders = GetPendingOrders(newInstance.Id);

            UpdateAccount(newInstance, activeOrders, pendingOrders);

            return newInstance;
        }
        
        private void UpdateAccount(IMarginTradingAccount account,
            ICollection<Order> activeOrders,
            ICollection<Order> pendingOrders)
        {
            var accuracy = _assetsCache.GetAssetAccuracy(account.BaseAssetId);
            var activeOrdersMaintenanceMargin = activeOrders.Sum(item => item.GetMarginMaintenance());
            var activeOrdersInitMargin = activeOrders.Sum(item => item.GetMarginInit());
            var pendingOrdersMargin = 0;// pendingOrders.Sum(item => item.GetMarginInit());

            account.AccountFpl.PnL = Math.Round(activeOrders.Sum(x => x.GetTotalFpl()), accuracy);

            account.AccountFpl.UsedMargin = Math.Round(activeOrdersMaintenanceMargin + pendingOrdersMargin, accuracy);
            account.AccountFpl.MarginInit = Math.Round(activeOrdersInitMargin + pendingOrdersMargin, accuracy);
            account.AccountFpl.OpenPositionsCount = activeOrders.Count;

            var tradingCondition = _tradingConditionsCache.GetTradingCondition(account.TradingConditionId);

            account.AccountFpl.MarginCallLevel = tradingCondition.MarginCall1;
            account.AccountFpl.StopoutLevel = tradingCondition.StopOut;
            account.AccountFpl.CalculatedHash = account.AccountFpl.ActualHash;
        }

        private ICollection<Order> GetActiveOrders(string accountId)
        {
            return _ordersCache.ActiveOrders.GetOrdersByAccountIds(accountId);
        }
        
        private ICollection<Order> GetPendingOrders(string accountId)
        {
            return _ordersCache.WaitingForExecutionOrders.GetOrdersByAccountIds(accountId);
        }
    }
}
