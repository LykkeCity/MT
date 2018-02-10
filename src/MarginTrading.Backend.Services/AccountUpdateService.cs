using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.TradingConditions;

namespace MarginTrading.Backend.Services
{
    public class AccountUpdateService : IAccountUpdateService
    {
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IAccountGroupCacheService _accountGroupCacheService;
        private readonly IAccountAssetsCacheService _accountAssetsCacheService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        private readonly IAssetsCache _assetsCache;

        public AccountUpdateService(
            ICfdCalculatorService cfdCalculatorService,
            IAccountGroupCacheService accountGroupCacheService,
            IAccountAssetsCacheService accountAssetsCacheService,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IAssetsCache assetsCache)
        {
            _cfdCalculatorService = cfdCalculatorService;
            _accountGroupCacheService = accountGroupCacheService;
            _accountAssetsCacheService = accountAssetsCacheService;
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
            var volumeInAccountAsset = _cfdCalculatorService.GetVolumeInAccountAsset(order.GetOrderType(), order.AccountAssetId, order.Instrument, Math.Abs(order.Volume));
            var account = _accountsCacheService.Get(order.ClientId, order.AccountId);
            var accountAsset = _accountAssetsCacheService.GetAccountAsset(order.TradingConditionId, order.AccountAssetId, order.Instrument);

            return account.GetMarginAvailable() * accountAsset.LeverageInit >= volumeInAccountAsset;
        }

        public MarginTradingAccount GuessAccountWithNewActiveOrder(Order order)
        {
            var newInstance = MarginTradingAccount.Create(_accountsCacheService.Get(order.ClientId, order.AccountId));

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
            //    orders = .ToArray();
            var accuracy = _assetsCache.GetAssetAccuracy(account.BaseAssetId);
            var activeOrdersMaintenanceMargin = activeOrders.Sum(item => item.GetMarginMaintenance());
            var activeOrdersInitMargin = activeOrders.Sum(item => item.GetMarginInit());
            var pendingOrdersMargin = pendingOrders.Sum(item => item.GetMarginInit());

            account.FplData.PnL = Math.Round(activeOrders.Sum(x => x.GetTotalFpl()), accuracy);

            account.FplData.UsedMargin = Math.Round(activeOrdersMaintenanceMargin + pendingOrdersMargin, accuracy);
            account.FplData.MarginInit = Math.Round(activeOrdersInitMargin + pendingOrdersMargin, accuracy);
            account.FplData.OpenPositionsCount = activeOrders.Count;

            var accountGroup =
                _accountGroupCacheService.GetAccountGroup(account.TradingConditionId, account.BaseAssetId);

            if (accountGroup == null)
            {
                throw new Exception(string.Format(MtMessages.AccountGroupForTradingConditionNotFound,
                    account.TradingConditionId, account.BaseAssetId));
            }

            account.FplData.MarginCallLevel = accountGroup.MarginCall;
            account.FplData.StopoutLevel = accountGroup.StopOut;
            account.FplData.CalculatedHash = account.FplData.ActualHash;
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
