﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Snow.Common.Costs;
using Lykke.Snow.Common.Percents;
using Lykke.Snow.Common.Quotes;
using MarginTrading.AssetService.Contracts.ClientProfileSettings;
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

        public async Task FreezeWithdrawalMargin(string accountId, string operationId, decimal amount)
        {
            var account = _accountsProvider.GetAccountById(accountId);

            if (account.AccountFpl.WithdrawalFrozenMarginData.TryAdd(operationId, amount))
            {
                account.AccountFpl.WithdrawalFrozenMargin = account.AccountFpl.WithdrawalFrozenMarginData.Values.Sum();
            }
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

        public void CheckIsEnoughBalance(Order order, IMatchingEngineBase matchingEngine, decimal additionalMargin)
        {
            var account = _accountsProvider.GetAccountById(order.AccountId);

            if (account == null)
                throw new InvalidOperationException($"Account with id {order.AccountId} not found");
            
            ThrowIfClientProfileSettingsInvalid(order.AssetPairId, account.TradingConditionId);
            
            var (entryCost, exitCost) = CalculateCosts(order, account.TradingConditionId);
            
            var marginAvailable = account.GetMarginAvailable() + additionalMargin;
            
            var orderMargin = _fplService.GetInitMarginForOrder(order);
            
            var pnlAtExecution = CalculatePnlAtExecution(order, matchingEngine);
            
            var orderBalanceAvailable = new OrderBalanceAvailable(marginAvailable, pnlAtExecution, entryCost, exitCost);

            if (orderBalanceAvailable < orderMargin)
                throw new ValidateOrderException(OrderRejectReason.NotEnoughBalance,
                    MtMessages.Validation_NotEnoughBalance,
                    $"Account available margin: {marginAvailable}, order margin: {orderMargin}, pnl at execution: {pnlAtExecution}, entry cost: {entryCost}, exit cost: {exitCost} ");
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

        private ICollection<Position> GetPositions(string accountId) =>
            _positionsProvider.GetPositionsByAccountIds(accountId);

        private ICollection<Order> GetActiveOrders(string accountId) =>
            _ordersProvider.GetActiveOrdersByAccountIds(accountId);
        
        private decimal CalculatePnlAtExecution(Order order, IMatchingEngineBase matchingEngine)
        {
            var quote = _quoteCacheService.GetQuote(order.AssetPairId);
            
            decimal openPrice;
            decimal closePrice;
            var directionForClose = order.Volume.GetClosePositionOrderDirection();

            if (quote.GetVolumeForOrderDirection(order.Direction) >= Math.Abs(order.Volume) &&
                quote.GetVolumeForOrderDirection(directionForClose) >= Math.Abs(order.Volume))
            {
                closePrice = quote.GetPriceForOrderDirection(directionForClose);
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
                openPrice = openPriceInfo.price.Value;

            }

            var pnlInTradingCurrency = (closePrice - openPrice) * order.Volume;
            var fxRate = _cfdCalculatorService.GetQuoteRateForQuoteAsset(order.AccountAssetId,
                order.AssetPairId, order.LegalEntity,
                pnlInTradingCurrency > 0);
            
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

        private (EntryCost, ExitCost) CalculateCosts(Order order, string tradingConditionId)
        {
            var quote = _quoteCacheService.GetQuote(order.AssetPairId).ToMathModel();

            var tradingInstrument = _tradingInstrumentsCache.GetTradingInstrument(tradingConditionId, order.AssetPairId);

            var executionPrice = order.Price.HasValue
                ? new ExecutionPrice(order.Price.Value)
                : quote.GetOrderExecutionPrice((Lykke.Snow.Common.Costs.OrderDirection)order.Direction);

            var spread = quote.Spread == Spread.Zero
                ? new Spread(tradingInstrument.Spread)
                : quote.Spread;

            return (
                new EntryCost(executionPrice, spread, GetCostConfiguration),
                new ExitCost(executionPrice, spread, GetCostConfiguration));

            (HedgeCost hedgeCost, decimal defaultCcVolume, DonationShare donationShare) GetCostConfiguration() => (
                new HedgeCost(tradingInstrument.HedgeCost),
                _marginTradingSettings.BrokerDefaultCcVolume,
                new DonationShare(_marginTradingSettings.BrokerDonationShare)
            );
        }

        private void ThrowIfClientProfileSettingsInvalid(string assetPairId, string tradingConditionId)
        {
            var assetType = _assetPairsCache.GetAssetPairById(assetPairId).AssetType;
            
            if (!_clientProfileSettingsCache.TryGetValue(tradingConditionId, assetType, out _))
                throw new InvalidOperationException($"Client profile settings for [{tradingConditionId}] and asset type [{assetType}] were not found in cache");
        }
    }
}
