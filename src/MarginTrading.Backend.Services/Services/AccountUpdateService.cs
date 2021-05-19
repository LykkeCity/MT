// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.TradingConditions;

#pragma warning disable 1998

namespace MarginTrading.Backend.Services.Services
{
    [UsedImplicitly]
    public class AccountUpdateService : IAccountUpdateService
    {
        private readonly IFplService _fplService;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly OrdersCache _ordersCache;
        
        private readonly ILog _log;
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly MarginTradingSettings _marginTradingSettings;

        public AccountUpdateService(
            IFplService fplService,
            IAccountsCacheService accountsCacheService,
            OrdersCache ordersCache,
            ILog log,
            ICfdCalculatorService cfdCalculatorService,
            IQuoteCacheService quoteCacheService,
            MarginTradingSettings marginTradingSettings)
        {
            _fplService = fplService;
            _accountsCacheService = accountsCacheService;
            _ordersCache = ordersCache;
            _log = log;
            _cfdCalculatorService = cfdCalculatorService;
            _quoteCacheService = quoteCacheService;
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
            }
        }

        public async Task UnfreezeWithdrawalMargin(string accountId, string operationId)
        {
            var account = _accountsCacheService.Get(accountId);
            
            if (account.AccountFpl.WithdrawalFrozenMarginData.TryRemove(operationId, out _))
            {
                account.AccountFpl.WithdrawalFrozenMargin = account.AccountFpl.WithdrawalFrozenMarginData.Values.Sum();
            }
        }

        public async Task FreezeUnconfirmedMargin(string accountId, string operationId, decimal amount)
        {
            var account = _accountsCacheService.Get(accountId);
            
            if (account.AccountFpl.UnconfirmedMarginData.TryAdd(operationId, amount))
            {
                account.AccountFpl.UnconfirmedMargin = account.AccountFpl.UnconfirmedMarginData.Values.Sum();
            }
        }

        public async Task UnfreezeUnconfirmedMargin(string accountId, string operationId)
        {
            var account = _accountsCacheService.Get(accountId);
            
            if (account.AccountFpl.UnconfirmedMarginData.TryRemove(operationId, out _))
            {
                account.AccountFpl.UnconfirmedMargin = account.AccountFpl.UnconfirmedMarginData.Values.Sum();
            }
        }

        public void CheckIsEnoughBalance(Order order, IMatchingEngineBase matchingEngine, decimal additionalMargin)
        {
            var orderMargin = _fplService.GetInitMarginForOrder(order);
            var accountMarginAvailable = _accountsCacheService.Get(order.AccountId).GetMarginAvailable() + additionalMargin;

            var quote = _quoteCacheService.GetQuote(order.AssetPairId);

            var openPrice = order.Price ?? 0;
            var closePrice = 0m;
            var directionForClose = order.Volume.GetClosePositionOrderDirection();

            if (quote.GetVolumeForOrderDirection(order.Direction) >= Math.Abs(order.Volume) &&
                quote.GetVolumeForOrderDirection(directionForClose) >= Math.Abs(order.Volume))
            {
                closePrice = quote.GetPriceForOrderDirection(directionForClose);

                if (openPrice == 0)
                    openPrice = quote.GetPriceForOrderDirection(order.Direction);
            }
            else
            {
                var openPriceInfo = matchingEngine.GetBestPriceForOpen(order.AssetPairId, order.Volume);
                var closePriceInfo =
                    matchingEngine.GetPriceForClose(order.AssetPairId, order.Volume, openPriceInfo.externalProviderId);

                if (openPriceInfo.price == null || closePriceInfo == null)
                {
                    throw new ValidateOrderException(OrderRejectReason.NoLiquidity,
                        "Price for open/close can not be calculated");
                }

                closePrice = closePriceInfo.Value;

                if (openPrice == 0)
                    openPrice = openPriceInfo.price.Value;

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
                throw new ValidateOrderFunctionalException(OrderRejectReason.NotEnoughBalance,
                    MtMessages.Validation_NotEnoughBalance,
                    $"Account available margin: {accountMarginAvailable}, order margin: {orderMargin}, pnl: {pnl} " +
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
            var accuracy = AssetsConstants.DefaultAssetAccuracy;
            var positionsMargin = positions.Sum(item => item.GetOvernightMarginMaintenance());
            var pendingOrdersMargin = 0;// pendingOrders.Sum(item => item.GetMarginInit());

            return Math.Round(positionsMargin + pendingOrdersMargin, accuracy);
        }

        private void UpdateAccount(IMarginTradingAccount account,
            ICollection<Position> positions,
            ICollection<Order> pendingOrders)
        {
            account.AccountFpl.CalculatedHash = account.AccountFpl.ActualHash;
            
            var accuracy = AssetsConstants.DefaultAssetAccuracy;
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

            account.AccountFpl.MarginCall1Level = _marginTradingSettings.DefaultTradingConditionsSettings.MarginCall1;
            account.AccountFpl.MarginCall2Level = _marginTradingSettings.DefaultTradingConditionsSettings.MarginCall2;
            account.AccountFpl.StopOutLevel = _marginTradingSettings.DefaultTradingConditionsSettings.StopOut;

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
