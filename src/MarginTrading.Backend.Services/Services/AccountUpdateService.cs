using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Backend.Core.Services;
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
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IQuoteCacheService _quoteCacheService;

        public AccountUpdateService(
            IFplService fplService,
            ITradingConditionsCacheService tradingConditionsCache,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            IAssetsCache assetsCache,
            IAccountMarginFreezingRepository accountMarginFreezingRepository,
            IAccountMarginUnconfirmedRepository accountMarginUnconfirmedRepository,
            ILog log,
            MarginTradingSettings marginTradingSettings,
            ICfdCalculatorService cfdCalculatorService,
            IQuoteCacheService quoteCacheService)
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
            _cfdCalculatorService = cfdCalculatorService;
            _quoteCacheService = quoteCacheService;
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

        public void CheckIsEnoughBalance(Order order, IMatchingEngineBase matchingEngine)
        {
            var orderMargin = _fplService.GetInitMarginForOrder(order);
            var accountMarginAvailable = _accountsCacheService.Get(order.AccountId).GetMarginAvailable();

            var quote = _quoteCacheService.GetQuote(order.AssetPairId);

            var openPrice = order.Price ?? 0;
            var closePrice = 0m;
            var directionForClose = order.Volume.GetClosePositionOrderDirection();

            if (quote.GetVolumeForOrderDirection(directionForClose) >= Math.Abs(order.Volume))
            {
                closePrice = quote.GetPriceForOrderDirection(directionForClose);
            }
            else
            {
                var openPriceInfo = matchingEngine.GetBestPriceForOpen(order.AssetPairId, order.Volume);
                var closePriceInfo =
                    matchingEngine.GetPriceForClose(order.AssetPairId, order.Volume, openPriceInfo.externalProviderId);

                if (openPriceInfo.price == null || closePriceInfo == null)
                {
                    throw new ValidateOrderException(OrderRejectReason.NoLiquidity,
                        "Price for open can not be calculated");
                }

                closePrice = closePriceInfo.Value;

                if (openPrice == 0)
                    openPrice = openPriceInfo.price.Value;

            }

            if (openPrice == 0)
            {
                if (quote.GetVolumeForOrderDirection(order.Direction) >= Math.Abs(order.Volume))
                {
                    openPrice = quote.GetPriceForOrderDirection(order.Direction);
                }
            }

            var pnlInTradingCurrency = (closePrice - openPrice) * order.Volume;
            var fxRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.AccountAssetId,
                order.AssetPairId, order.LegalEntity,
                pnlInTradingCurrency > 0);
            var pnl = pnlInTradingCurrency * fxRate;

            // just in case... is should be always negative
            if (pnl > 0)
            {
                _log.WriteWarning(nameof(CheckIsEnoughBalance), order.ToJson(),
                    $"Theoretical PnL at the moment of order execution is positive");
                pnl = 0;
            }

            if (accountMarginAvailable + pnl < orderMargin)
                throw new ValidateOrderException(OrderRejectReason.NotEnoughBalance,
                    MtMessages.Validation_NotEnoughBalance,
                    $"Account available margin: {accountMarginAvailable}, order margin: {orderMargin}, pnl: {pnl}d " +
                    $"(open price: {openPrice}, close price: {closePrice}, fx rate: {fxRate})");
        }

        public void RemoveLiquidationStateIfNeeded(string accountId, string reason,
            string liquidationOperationId = null, LiquidationType liquidationType = LiquidationType.Normal)
        {
            var account = _accountsCacheService.TryGet(accountId);

            if (account == null)
                return;

            if (!string.IsNullOrEmpty(account.LiquidationOperationId)
                && (liquidationType == LiquidationType.Forced 
                    || account.GetAccountLevel() != AccountLevel.StopOut))
            {
                _accountsCacheService.TryFinishLiquidation(accountId, reason, liquidationOperationId);
            }
        }

        public decimal CalculateOvernightUsedMargin(IMarginTradingAccount account)
        {
            var positions = GetPositions(account.Id);
            var accuracy = _assetsCache.GetAssetAccuracy(account.BaseAssetId);
            var positionsMargin = positions.Sum(item => item.GetOvernightMarginMaintenance());
            var pendingOrdersMargin = 0;// pendingOrders.Sum(item => item.GetMarginInit());

            return Math.Round(positionsMargin + pendingOrdersMargin, accuracy);
        }

        private void UpdateAccount(IMarginTradingAccount account,
            ICollection<Position> positions,
            ICollection<Order> pendingOrders)
        {
            account.AccountFpl.CalculatedHash = account.AccountFpl.ActualHash;
            
            var accuracy = _assetsCache.GetAssetAccuracy(account.BaseAssetId);
            var positionsMaintenanceMargin = positions.Sum(item => item.GetMarginMaintenance());
            var positionsInitMargin = positions.Sum(item => item.GetMarginInit());
            var pendingOrdersMargin = 0;// pendingOrders.Sum(item => item.GetMarginInit());

            account.AccountFpl.PnL = Math.Round(positions.Sum(x => x.GetTotalFpl()), accuracy);
            account.AccountFpl.UnrealizedDailyPnl =
                Math.Round(positions.Sum(x => x.GetTotalFpl() - x.ChargedPnL), accuracy);

            account.AccountFpl.UsedMargin = Math.Round(positionsMaintenanceMargin + pendingOrdersMargin, accuracy);
            account.AccountFpl.MarginInit = Math.Round(positionsInitMargin + pendingOrdersMargin, accuracy);
            account.AccountFpl.InitiallyUsedMargin = positions.Sum(p => p.GetInitialMargin());
            account.AccountFpl.OpenPositionsCount = positions.Count;
            account.AccountFpl.ActiveOrdersCount = pendingOrders.Count;

            var tradingCondition = _tradingConditionsCache.GetTradingCondition(account.TradingConditionId);

            account.AccountFpl.MarginCall1Level = tradingCondition.MarginCall1;
            account.AccountFpl.MarginCall2Level = tradingCondition.MarginCall2;
            account.AccountFpl.StopOutLevel = tradingCondition.StopOut;
           
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
