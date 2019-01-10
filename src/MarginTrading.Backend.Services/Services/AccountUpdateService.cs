using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Assets;
using MarginTrading.Backend.Services.TradingConditions;
#pragma warning disable 1998

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
        
        private readonly IAccountMarginFreezingRepository _accountMarginFreezingRepository;
        private readonly IAccountMarginUnconfirmedRepository _accountMarginUnconfirmedRepository;
        private readonly ILog _log;
        private readonly MarginTradingSettings _marginTradingSettings;

        public AccountUpdateService(
            IFplService fplService,
            ITradingConditionsCacheService tradingConditionsCache,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IAssetsCache assetsCache,
            IAccountMarginFreezingRepository accountMarginFreezingRepository,
            IAccountMarginUnconfirmedRepository accountMarginUnconfirmedRepository,
            ILog log,
            MarginTradingSettings marginTradingSettings)
        {
            _fplService = fplService;
            _tradingConditionsCache = tradingConditionsCache;
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _assetsCache = assetsCache;
            _accountMarginFreezingRepository = accountMarginFreezingRepository;
            _accountMarginUnconfirmedRepository = accountMarginUnconfirmedRepository;
            _log = log;
            _marginTradingSettings = marginTradingSettings;
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
                //TODO: think about approach
                //await _accountMarginFreezingRepository.TryInsertAsync(new AccountMarginFreezing(operationId,
                //    accountId, amount));
            }
        }

        public async Task UnfreezeWithdrawalMargin(string accountId, string operationId)
        {
            var account = _accountsCacheService.Get(accountId);
            
            if (account.AccountFpl.WithdrawalFrozenMarginData.Remove(operationId))
            {
                account.AccountFpl.WithdrawalFrozenMargin = account.AccountFpl.WithdrawalFrozenMarginData.Values.Sum();
                //TODO: think about approach
                //await _accountMarginFreezingRepository.DeleteAsync(operationId);
            }
        }

        public async Task FreezeUnconfirmedMargin(string accountId, string operationId, decimal amount)
        {
            var account = _accountsCacheService.Get(accountId);
            
            if (account.AccountFpl.UnconfirmedMarginData.TryAdd(operationId, amount))
            {
                account.AccountFpl.UnconfirmedMargin = account.AccountFpl.UnconfirmedMarginData.Values.Sum();
                //TODO: think about approach
                //await _accountMarginUnconfirmedRepository.TryInsertAsync(new AccountMarginFreezing(operationId,
                //    accountId, amount));
            }
        }

        public async Task UnfreezeUnconfirmedMargin(string accountId, string operationId)
        {
            var account = _accountsCacheService.Get(accountId);
            
            if (account.AccountFpl.UnconfirmedMarginData.Remove(operationId))
            {
                account.AccountFpl.UnconfirmedMargin = account.AccountFpl.UnconfirmedMarginData.Values.Sum();
                //TODO: think about approach
                //await _accountMarginUnconfirmedRepository.DeleteAsync(operationId);
            }
        }

        public bool IsEnoughBalance(Order order)
        {
            var orderMargin = _fplService.GetInitMarginForOrder(order);
            var accountMarginAvailable = _accountsCacheService.Get(order.AccountId).GetMarginAvailable(); 
            
            return accountMarginAvailable >= orderMargin;
        }
        
        public void RemoveLiquidationStateIfNeeded(string accountId, string reason,
            string liquidationOperationId = null)
        {
            var account = _accountsCacheService.TryGet(accountId);

            if (account == null)
                return;

            if (!string.IsNullOrEmpty(account.LiquidationOperationId) &&
                account.GetAccountLevel() != AccountLevel.StopOut)
            {
                _accountsCacheService.TryFinishLiquidation(accountId, reason, liquidationOperationId);
            }
        }

        public decimal CalculateOvernightUsedMargin(IMarginTradingAccount account)
        {
            var positions = GetPositions(account.Id);
            var accuracy = _assetsCache.GetAssetAccuracy(account.BaseAssetId);
            var activeOrdersMaintenanceMargin = positions.Sum(item => item.GetOvernightMarginMaintenance());
            var pendingOrdersMargin = 0;// pendingOrders.Sum(item => item.GetMarginInit());

            return Math.Round(activeOrdersMaintenanceMargin + pendingOrdersMargin, accuracy);
        }
        
        private void UpdateAccount(IMarginTradingAccount account,
            ICollection<Position> positions,
            ICollection<Order> pendingOrders)
        {
            account.AccountFpl.CalculatedHash = account.AccountFpl.ActualHash;
            
            var accuracy = _assetsCache.GetAssetAccuracy(account.BaseAssetId);
            var activeOrdersMaintenanceMargin = positions.Sum(item => item.GetMarginMaintenance());
            var activeOrdersInitMargin = positions.Sum(item => item.GetMarginInit());
            var pendingOrdersMargin = 0;// pendingOrders.Sum(item => item.GetMarginInit());

            account.AccountFpl.PnL = Math.Round(positions.Sum(x => x.GetTotalFpl()), accuracy);
            account.AccountFpl.UnrealizedDailyPnl =
                Math.Round(positions.Sum(x => x.GetTotalFpl() - x.ChargedPnL), accuracy);

            account.AccountFpl.UsedMargin = Math.Round(activeOrdersMaintenanceMargin + pendingOrdersMargin, accuracy);
            account.AccountFpl.MarginInit = Math.Round(activeOrdersInitMargin + pendingOrdersMargin, accuracy);
            account.AccountFpl.OpenPositionsCount = positions.Count;
            account.AccountFpl.ActiveOrdersCount = pendingOrders.Count;

            var tradingCondition = _tradingConditionsCache.GetTradingCondition(account.TradingConditionId);

            account.AccountFpl.MarginCall1Level = tradingCondition.MarginCall1;
            account.AccountFpl.MarginCall2Level = tradingCondition.MarginCall2;
            account.AccountFpl.StopOutLevel = tradingCondition.StopOut;

            UpdateMcoLevel(account, positions);
        }

        private void UpdateMcoLevel(IMarginTradingAccount account, ICollection<Position> positions)
        {
            if (_marginTradingSettings.McoRules == null)
                return;
            
            var initialMarginLong = 0m;
            var initialMarginShort = 0m;
            var currentMarginLong = 0m;
            var currentMarginShort = 0m;

            foreach (var position in positions)
            {
                if (position.Direction == PositionDirection.Long)
                {
                    initialMarginLong += position.GetMcoInitialMargin();
                    currentMarginLong += position.GetMcoCurrentMargin();
                }

                if (position.Direction == PositionDirection.Short)
                {
                    initialMarginShort += position.GetMcoInitialMargin();
                    currentMarginShort += position.GetMcoCurrentMargin();
                }
            }

            account.AccountFpl.McoMarginUsageLevelLong = initialMarginLong > 0
                ? currentMarginLong / initialMarginLong
                : 0;

            account.AccountFpl.McoMarginUsageLevelShort = initialMarginShort > 0
                ? currentMarginShort / initialMarginShort
                : 0;
        }

        private ICollection<Position> GetPositions(string accountId)
        {
            return _ordersCache.Positions.GetPositionsByAccountIds(accountId);
        }
        
        private ICollection<Order> GetActiveOrders(string accountId)
        {
            return _ordersCache.Active.GetOrdersByAccountIds(accountId);
        }
    }
}
