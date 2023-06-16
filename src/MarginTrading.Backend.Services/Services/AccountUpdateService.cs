// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Snow.Common.Costs;
using Lykke.Snow.Common.Percents;
using MarginTrading.AssetService.Contracts.ClientProfileSettings;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Infrastructure;
using MarginTrading.Backend.Services.TradingConditions;

#pragma warning disable 1998

namespace MarginTrading.Backend.Services.Services
{
    [UsedImplicitly]
    public class AccountUpdateService : IAccountUpdateService
    {
        private readonly IFplService _fplService;

        private readonly ILog _log;
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IQuoteCacheService _quoteCacheService;
        private readonly MarginTradingSettings _marginTradingSettings;
        private readonly IClientProfileSettingsCache _clientProfileSettingsCache;
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly ITradingInstrumentsCacheService _tradingInstrumentsCache;

        private readonly IOrdersProvider _ordersProvider;
        private readonly IPositionsProvider _positionsProvider;
        private readonly IAccountsProvider _accountsProvider;
        private readonly IAccountsCacheService _accountsCacheService;

        public AccountUpdateService(
            IFplService fplService,
            OrdersCache ordersCache,
            ILog log,
            ICfdCalculatorService cfdCalculatorService,
            IQuoteCacheService quoteCacheService,
            MarginTradingSettings marginTradingSettings,
            IClientProfileSettingsCache clientProfileSettingsCache,
            IAssetPairsCache assetPairsCache,
            IPositionsProvider positionsProvider,
            IOrdersProvider ordersProvider,
            IAccountsProvider accountsProvider,
            ITradingInstrumentsCacheService tradingInstrumentsCache,
            IAccountsCacheService accountsCacheService)
        {
            _fplService = fplService;
            _log = log;
            _cfdCalculatorService = cfdCalculatorService;
            _quoteCacheService = quoteCacheService;
            _marginTradingSettings = marginTradingSettings;
            _clientProfileSettingsCache = clientProfileSettingsCache;
            _assetPairsCache = assetPairsCache;
            _positionsProvider = positionsProvider;
            _ordersProvider = ordersProvider;
            _accountsProvider = accountsProvider;
            _tradingInstrumentsCache = tradingInstrumentsCache;
            _accountsCacheService = accountsCacheService;
        }

        public void UpdateAccount(IMarginTradingAccount account)
        {
            UpdateAccount(account, GetPositions(account.Id), GetActiveOrders(account.Id));
        }

        public decimal FreezeWithdrawalMargin(string accountId, string operationId, decimal amount)
        {
            var account = _accountsProvider.GetAccountById(accountId);

            if (account.AccountFpl.WithdrawalFrozenMarginData.TryAdd(operationId, amount))
            {
                account.AccountFpl.WithdrawalFrozenMargin = account.AccountFpl.WithdrawalFrozenMarginData.Values.Sum();
            }

            return account.AccountFpl.WithdrawalFrozenMargin;
        }

        public async Task UnfreezeWithdrawalMargin(string accountId, string operationId)
        {
            var account = _accountsProvider.GetAccountById(accountId);

            if (account.AccountFpl.WithdrawalFrozenMarginData.TryRemove(operationId, out _))
            {
                account.AccountFpl.WithdrawalFrozenMargin = account.AccountFpl.WithdrawalFrozenMarginData.Values.Sum();
            }
        }

        public async Task FreezeUnconfirmedMargin(string accountId, string operationId, decimal amount)
        {
            var account = _accountsProvider.GetAccountById(accountId);

            if (account.AccountFpl.UnconfirmedMarginData.TryAdd(operationId, amount))
            {
                account.AccountFpl.UnconfirmedMargin = account.AccountFpl.UnconfirmedMarginData.Values.Sum();
            }
        }

        public async Task UnfreezeUnconfirmedMargin(string accountId, string operationId)
        {
            var account = _accountsProvider.GetAccountById(accountId);

            if (account.AccountFpl.UnconfirmedMarginData.TryRemove(operationId, out _))
            {
                account.AccountFpl.UnconfirmedMargin = account.AccountFpl.UnconfirmedMarginData.Values.Sum();
            }
        }

        public void CheckBalance(OrderFulfillmentPlan orderFulfillmentPlan, IMatchingEngineBase matchingEngine)
        {
            var account = _accountsProvider.GetAccountById(orderFulfillmentPlan.Order.AccountId);

            if (account == null)
                throw new InvalidOperationException($"Account with id {orderFulfillmentPlan.Order.AccountId} not found");

            var assetType = _assetPairsCache.GetAssetPairById(orderFulfillmentPlan.Order.AssetPairId).AssetType;
            
            if (!_clientProfileSettingsCache.TryGetValue(account.TradingConditionId, assetType, out var clientProfileSettings))
                throw new InvalidOperationException($"Client profile settings for [{account.TradingConditionId}] and asset type [{assetType}] were not found in cache");

            var (openPrice, closePrice) = GetPrices(
                orderFulfillmentPlan.Order,
                orderFulfillmentPlan.UnfulfilledVolume,
                matchingEngine);

            var pnlInTradingCurrency = (closePrice - openPrice) * orderFulfillmentPlan.UnfulfilledVolume;

            var fxRate = GetFxRate(orderFulfillmentPlan.Order, pnlInTradingCurrency);

            // orderFulfillmentPlan.Order.Volume is OrderSize in this case that's why price is included
            var entryCost = new EntryCost(new EntryCommissionCost(clientProfileSettings.ExecutionFeesFloor,
                    new ExecutionFeeRate(clientProfileSettings.ExecutionFeesRate), 
                    clientProfileSettings.ExecutionFeesCap, 
                    fxRate, 
                    orderFulfillmentPlan.Order.Volume * openPrice));
            
            // orderFulfillmentPlan.UnfulfilledVolume is OrderSize in this case that's why price is included
            var exitCost = new ExitCost(new ExitCommissionCost(clientProfileSettings.ExecutionFeesFloor,
                new ExecutionFeeRate(clientProfileSettings.ExecutionFeesRate),
                clientProfileSettings.ExecutionFeesCap,
                fxRate,
                orderFulfillmentPlan.UnfulfilledVolume * openPrice));

            var marginAvailable = account.GetMarginAvailable() + (orderFulfillmentPlan.OppositePositionsState?.Margin ?? 0);
            
            var orderMargin = _fplService.GetInitMarginForOrder(orderFulfillmentPlan.Order, orderFulfillmentPlan.UnfulfilledVolume);

            var pnlAtExecution = CalculatePnlAtExecution(orderFulfillmentPlan.Order, pnlInTradingCurrency);
            
            var orderBalanceAvailable = new OrderBalanceAvailable(marginAvailable, pnlAtExecution, entryCost, exitCost);

            _log.WriteInfo(nameof(CheckBalance),
                new
                {
                    orderFulfillmentPlan.Order, 
                    entryCost = (decimal) entryCost, 
                    exitCost = (decimal) exitCost,
                    marginAvailable, 
                    pnlAtExecution, 
                    orderMargin, 
                    orderBalanceAvailable = (decimal) orderBalanceAvailable
                }.ToJson(),
                $"Calculation made on order");

            if (orderBalanceAvailable < orderMargin)
                throw new OrderRejectionException(OrderRejectReason.NotEnoughBalance,
                    MtMessages.Validation_NotEnoughBalance,
                    $"Account available margin: {marginAvailable}, order margin: {orderMargin}, pnl at execution: {pnlAtExecution}, entry cost: {(decimal)entryCost}, exit cost: {(decimal)exitCost} ");
        }

        public async ValueTask RemoveLiquidationStateIfNeeded(string accountId,
            string reason,
            string liquidationOperationId = null,
            LiquidationType liquidationType = LiquidationType.Normal)
        {
            var account = _accountsProvider.GetAccountById(accountId);

            if (account == null)
                return;

            var isInLiquidation = await _accountsCacheService.IsInLiquidation(accountId);

            if (isInLiquidation && (liquidationType == LiquidationType.Forced
                                    || account.GetAccountLevel() != AccountLevel.StopOut))
            {
                await _accountsProvider.TryFinishLiquidation(accountId, reason, liquidationOperationId);
            }
        }

        public decimal CalculateOvernightUsedMargin(IMarginTradingAccount account)
        {
            var positions = GetPositions(account.Id);
            var accuracy = AssetsConstants.DefaultAssetAccuracy;
            var positionsMargin = positions.Sum(item => item.GetOvernightMarginMaintenance());
            var pendingOrdersMargin = 0;

            return Math.Round(positionsMargin + pendingOrdersMargin, accuracy);
        }

        private void UpdateAccount(IMarginTradingAccount account,
            ICollection<Position> positions,
            ICollection<Order> pendingOrders)
        {
            account.AccountFpl.CalculatedHash = account.AccountFpl.ActualHash;

            var accuracy = AssetsConstants.DefaultAssetAccuracy;
            var positionsMaintenanceMarginValues = positions.Select(item => item.GetMarginMaintenance());
            var positionsMaintenanceMargin = positionsMaintenanceMarginValues.Sum();

            var positionsInitMargin = positions.Sum(item => item.GetMarginInit());
            var pendingOrdersMargin = 0;

            account.AccountFpl.PnL = Math.Round(positions.Sum(x => x.GetTotalFpl()), accuracy);
            account.AccountFpl.UnrealizedDailyPnl =
                Math.Round(positions.Sum(x => x.GetTotalFpl() - x.ChargedPnL), accuracy);

            account.AccountFpl.UsedMargin = Math.Round(positionsMaintenanceMargin + pendingOrdersMargin, accuracy);

            if (_marginTradingSettings.LogBlockedMarginCalculation)
            {
                var positionsMaintenanceMarginLog = string.Join(" + ", positions.Select(item => $"posId: {item.Id}, {item.GetMarginMaintenance().ToString(CultureInfo.InvariantCulture)}"));

                account.LogInfo = @$"PositionsMaintenanceMargin: {positionsMaintenanceMargin} = {positionsMaintenanceMarginLog}. 
                    Summed values: {positionsMaintenanceMargin.ToJson()} - LastUpdate: {DateTime.UtcNow}";
            }
            
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
            var positions = _positionsProvider.GetPositionsByAccountIds(accountId);

            if(_marginTradingSettings.LogBlockedMarginCalculation && SnapshotService.IsMakingSnapshotInProgress)
            {
                _log.WriteInfo(nameof(AccountUpdateService), positions?.Select(p => new { p.Id, p.AssetPairId, p.ClosePrice, p.CloseFxPrice }).ToJson(), 
                    $"Account {accountId} - Position array from position provider");
            }

            return positions;
        }

        private ICollection<Order> GetActiveOrders(string accountId) =>
            _ordersProvider.GetActiveOrdersByAccountIds(accountId);

        private (decimal, decimal) GetPrices(Order order, decimal actualVolume, IMatchingEngineBase matchingEngine)
        {
            var quote = _quoteCacheService.GetQuote(order.AssetPairId);
            
            decimal openPrice;
            decimal closePrice;
            
            var directionForClose = order.Volume.GetClosePositionOrderDirection();

            if (quote.GetVolumeForOrderDirection(order.Direction) >= Math.Abs(actualVolume) &&
                quote.GetVolumeForOrderDirection(directionForClose) >= Math.Abs(actualVolume))
            {
                closePrice = quote.GetPriceForOrderDirection(directionForClose);
                openPrice = quote.GetPriceForOrderDirection(order.Direction);
            }
            else
            {
                var openPriceInfo = matchingEngine.GetBestPriceForOpen(order.AssetPairId, actualVolume);
                var closePriceInfo = matchingEngine.GetPriceForClose(order.AssetPairId, actualVolume, openPriceInfo.externalProviderId);

                if (openPriceInfo.price == null || closePriceInfo == null)
                {
                    throw new OrderRejectionException(OrderRejectReason.NoLiquidity,
                        "Price for open/close can not be calculated");
                }

                closePrice = closePriceInfo.Value;
                openPrice = openPriceInfo.price.Value;
            }

            return (openPrice, closePrice);
        }
        
        private decimal CalculatePnlAtExecution(Order order, decimal pnlInTradingCurrency)
        {
            var fxRate = GetFxRate(order, pnlInTradingCurrency);
            
            var result = pnlInTradingCurrency * fxRate;

            // just in case... is should be always negative
            if (result > 0)
            {
                _log.WriteWarning(nameof(CalculatePnlAtExecution), order.ToJson(),
                    $"Theoretical PnL at the moment of order execution is positive");

                result = 0;
            }

            return result;
        }

        private decimal GetFxRate(Order order, decimal pnlInTradingCurrency)
        {
            var fxRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.AccountAssetId,
                order.AssetPairId,
                order.LegalEntity,
                pnlInTradingCurrency > 0);
            return fxRate;
        }
    }
}
