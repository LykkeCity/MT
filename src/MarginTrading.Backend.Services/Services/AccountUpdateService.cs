using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;
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
            UpdateAccount(account, GetPositions(account.Id), GetActiveOrders(account.Id));
        }

        public async Task FreezeWithdrawalMargin(string accountId, string operationId, decimal amount)
        {
            var account = _accountsCacheService.Get(accountId);
            
            if (account.AccountFpl.WithdrawalFrozenMarginData.TryAdd(operationId, amount))
            {
                account.AccountFpl.WithdrawalFrozenMargin = account.AccountFpl.WithdrawalFrozenMarginData.Values.Sum();
                await _accountsCacheService.FreezeWithdrawalMargin(operationId, account.ClientId, accountId, amount);
            }
        }

        public async Task UnfreezeWithdrawalMargin(string accountId, string operationId)
        {
            var account = _accountsCacheService.Get(accountId);
            
            if (account.AccountFpl.WithdrawalFrozenMarginData.Remove(operationId))
            {
                account.AccountFpl.WithdrawalFrozenMargin = account.AccountFpl.WithdrawalFrozenMarginData.Values.Sum();
                await _accountsCacheService.UnfreezeWithdrawalMargin(operationId);
            }
        }

        public bool IsEnoughBalance(Order order)
        {
            var orderMargin = _fplService.GetInitMarginForOrder(order);
            var accountMarginAvailable = _accountsCacheService.Get(order.AccountId).GetMarginAvailable(); 
            
            return accountMarginAvailable >= orderMargin;
        }

        public MarginTradingAccount GuessAccountWithNewActiveOrder(Position order)
        {
            var newInstance = MarginTradingAccount.Create(_accountsCacheService.Get(order.AccountId));

            var activeOrders = GetPositions(newInstance.Id);
            activeOrders.Add(order);
            
            var pendingOrders = GetActiveOrders(newInstance.Id);

            UpdateAccount(newInstance, activeOrders, pendingOrders);

            return newInstance;
        }
        
        private void UpdateAccount(IMarginTradingAccount account,
            ICollection<Position> activeOrders,
            ICollection<Order> pendingOrders)
        {
            account.AccountFpl.CalculatedHash = account.AccountFpl.ActualHash;
            
            var accuracy = _assetsCache.GetAssetAccuracy(account.BaseAssetId);
            var activeOrdersMaintenanceMargin = activeOrders.Sum(item => item.GetMarginMaintenance());
            var activeOrdersInitMargin = activeOrders.Sum(item => item.GetMarginInit());
            var pendingOrdersMargin = 0;// pendingOrders.Sum(item => item.GetMarginInit());

            account.AccountFpl.PnL = Math.Round(activeOrders.Sum(x => x.GetTotalFpl()), accuracy);
            account.AccountFpl.UnrealizedDailyPnl =
                Math.Round(activeOrders.Sum(x => x.GetTotalFpl() - x.ChargedPnL), accuracy);

            account.AccountFpl.UsedMargin = Math.Round(activeOrdersMaintenanceMargin + pendingOrdersMargin, accuracy);
            account.AccountFpl.MarginInit = Math.Round(activeOrdersInitMargin + pendingOrdersMargin, accuracy);
            account.AccountFpl.OpenPositionsCount = activeOrders.Count;

            var tradingCondition = _tradingConditionsCache.GetTradingCondition(account.TradingConditionId);

            account.AccountFpl.MarginCall1Level = tradingCondition.MarginCall1;
            account.AccountFpl.MarginCall2Level = tradingCondition.MarginCall2;
            account.AccountFpl.StopoutLevel = tradingCondition.StopOut;
        }

        private ICollection<Position> GetPositions(string accountId)
        {
            return _ordersCache.Positions.GetOrdersByAccountIds(accountId);
        }
        
        private ICollection<Order> GetActiveOrders(string accountId)
        {
            return _ordersCache.Active.GetOrdersByAccountIds(accountId);
        }
    }
}
