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
using OrderDirection = MarginTrading.Backend.Core.Orders.OrderDirection;

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
            ITradingInstrumentsCacheService tradingInstrumentsCache)
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
            _log.WriteInfo(nameof(CheckIsEnoughBalance), new { Order = order, additionalMargin }.ToJson(),
                "Start checking if account balance is enough ...");
            
            var orderMargin = _fplService.GetInitMarginForOrder(order);
            _log.WriteInfo(nameof(CheckIsEnoughBalance), new {Order = order, orderMargin }.ToJson(),
                "Order margin calculated");
            
            var account =_accountsProvider.GetAccountById(order.AccountId);
            var accountMarginAvailable = account.GetMarginAvailable() + additionalMargin;
            _log.WriteInfo(nameof(CheckIsEnoughBalance), new {Order = order, Account = account, accountMarginAvailable }.ToJson(),
                "Account margin available calculated");

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
            _log.WriteInfo(nameof(CheckIsEnoughBalance), new {Order = order, Quote = quote, openPrice, closePrice }.ToJson(),
                "Open and close prices calculated");
            

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
            _log.WriteInfo(nameof(CheckIsEnoughBalance), new {Order = order, pnlInTradingCurrency, fxRate, pnl }.ToJson(),
                "PNL calculated");
            
            var assetType = _assetPairsCache.GetAssetPairById(order.AssetPairId).AssetType;
            if (!_clientProfileSettingsCache.TryGetValue(account.TradingConditionId, assetType, out var clientProfileSettings))
                throw new InvalidOperationException($"Client profile settings for [{account.TradingConditionId}] and asset type [{assetType}] were not found in cache");

            var tradingInstrument =
                _tradingInstrumentsCache.GetTradingInstrument(account.TradingConditionId, order.AssetPairId);

            var entryCost = CostHelper.CalculateEntryCost(
                order.Price,
                order.Direction == OrderDirection.Buy ? Lykke.Snow.Common.Costs.OrderDirection.Buy : Lykke.Snow.Common.Costs.OrderDirection.Sell,
                quote.Ask,
                quote.Bid,
                fxRate,
                tradingInstrument.Spread,
                tradingInstrument.HedgeCost,
                _marginTradingSettings.BrokerDefaultCcVolume,
                _marginTradingSettings.BrokerDonationShare);

            _log.WriteInfo(nameof(CheckIsEnoughBalance),
                new
                {
                    OrderPrice = order.Price, OrderDirection = order.Direction, quote.Ask, quote.Bid, fxRate,
                    tradingInstrument.Spread, tradingInstrument.HedgeCost, _marginTradingSettings.BrokerDefaultCcVolume,
                    _marginTradingSettings.BrokerDonationShare, CalculatedEntryCost = entryCost
                }.ToJson(),
                "Entry cost calculated");
            
            var exitCost = CostHelper.CalculateExitCost(
                order.Price,
                order.Direction == OrderDirection.Buy ? Lykke.Snow.Common.Costs.OrderDirection.Buy : Lykke.Snow.Common.Costs.OrderDirection.Sell,
                quote.Ask,
                quote.Bid,
                fxRate,
                tradingInstrument.Spread,
                tradingInstrument.HedgeCost,
                _marginTradingSettings.BrokerDefaultCcVolume,
                _marginTradingSettings.BrokerDonationShare);
            
            _log.WriteInfo(nameof(CheckIsEnoughBalance),
                new
                {
                    OrderPrice = order.Price, OrderDirection = order.Direction, quote.Ask, quote.Bid, fxRate,
                    tradingInstrument.Spread, tradingInstrument.HedgeCost, _marginTradingSettings.BrokerDefaultCcVolume,
                    _marginTradingSettings.BrokerDonationShare, CalculatedExitCost = exitCost
                }.ToJson(),
                "Exit cost calculated");

            if (accountMarginAvailable + pnl - entryCost - exitCost < orderMargin)
                throw new ValidateOrderException(OrderRejectReason.NotEnoughBalance,
                    MtMessages.Validation_NotEnoughBalance,
                    $"Account available margin: {accountMarginAvailable}, order margin: {orderMargin}, pnl: {pnl}, entry cost: {entryCost}, exit cost: {exitCost} " +
                    $"(open price: {openPrice}, close price: {closePrice}, fx rate: {fxRate})");
            
            _log.WriteInfo(nameof(CheckIsEnoughBalance), new { Order = order,  accountMarginAvailable, pnl, entryCost, exitCost, orderMargin}.ToJson(),
                "Account balance is enough, validation succeeded.");
        }

        public void RemoveLiquidationStateIfNeeded(string accountId, string reason,
            string liquidationOperationId = null, LiquidationType liquidationType = LiquidationType.Normal)
        {
            var account = _accountsProvider.GetAccountById(accountId);

            if (account == null)
                return;

            if (!string.IsNullOrEmpty(account.LiquidationOperationId)
                && (liquidationType == LiquidationType.Forced
                    || account.GetAccountLevel() != AccountLevel.StopOut))
            {
                _accountsProvider.TryFinishLiquidation(accountId, reason, liquidationOperationId);
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
            var positionsMaintenanceMarginLog = string.Join(" + ", positions.Select(item => item.GetMarginMaintenance().ToString(CultureInfo.InvariantCulture)));
            var positionsInitMargin = positions.Sum(item => item.GetMarginInit());
            var pendingOrdersMargin = 0;// pendingOrders.Sum(item => item.GetMarginInit());

            account.AccountFpl.PnL = Math.Round(positions.Sum(x => x.GetTotalFpl()), accuracy);
            account.AccountFpl.UnrealizedDailyPnl =
                Math.Round(positions.Sum(x => x.GetTotalFpl() - x.ChargedPnL), accuracy);

            account.AccountFpl.UsedMargin = Math.Round(positionsMaintenanceMargin + pendingOrdersMargin, accuracy);
            account.LogInfo = $"PositionsMaintenanceMargin: {positionsMaintenanceMargin} = {positionsMaintenanceMarginLog}";
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
    }
}
