// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Snapshots;
using MarginTrading.Backend.Core.Trading;
using MarginTrading.Backend.Services.Mappers;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services.Services
{
    /// <inheritdoc />
    public class FinalSnapshotCalculator : IFinalSnapshotCalculator
    {
        private readonly ICfdCalculatorService _cfdCalculatorService;
        private readonly IDateService _dateService;
        private readonly ILifetimeScope _lifetimeScope;
        private readonly ILog _log;

        public FinalSnapshotCalculator(ICfdCalculatorService cfdCalculatorService, ILog log, IDateService dateService, ILifetimeScope lifetimeScope)
        {
            _cfdCalculatorService = cfdCalculatorService;
            _log = log;
            _dateService = dateService;
            _lifetimeScope = lifetimeScope;
        }
        
        /// <inheritdoc />
        public async Task<TradingEngineSnapshot> RunAsync(IEnumerable<ClosingFxRate> fxRates, IEnumerable<ClosingAssetPrice> cfdQuotes, string correlationId)
        {
            var fxRatesList = fxRates?.ToList();
            var cfdQuotesList = cfdQuotes?.ToList();
            
            if (fxRatesList == null || !fxRatesList.Any())
                throw new ArgumentNullException(nameof(fxRates), @"FX rates can't be null or empty");
            
            if (cfdQuotesList == null || !cfdQuotesList.Any())
                throw new ArgumentNullException(nameof(cfdQuotes), @"CFD quotes can't be null or empty");
            
            using (var scope = _lifetimeScope.BeginLifetimeScope())
            {
                var snapshotKeeper = scope.Resolve<IDraftSnapshotKeeper>();

                var positions = snapshotKeeper.GetPositions();
                var accounts = (await snapshotKeeper.GetAccountsAsync()).ToImmutableArray();
                foreach (var closingFxRate in fxRatesList)
                {
                    ApplyFxRate(positions, accounts, closingFxRate.ClosePrice, closingFxRate.FhQuoterCode);
                }
                
                var orders = snapshotKeeper.GetAllOrders();
                foreach (var closingAssetPrice in cfdQuotesList)
                {
                    ApplyCfdQuote(positions, orders, accounts, closingAssetPrice.ClosePrice, closingAssetPrice.MdsCode);
                }

                await snapshotKeeper.UpdateAsync(positions, orders, accounts);

                var timestamp = _dateService.Now();

                return new TradingEngineSnapshot(snapshotKeeper.TradingDay,
                    correlationId,
                    timestamp,
                    MapToFinalJson(orders, snapshotKeeper),
                    MapToFinalJson(positions, snapshotKeeper),
                    MapToFinalJson(accounts),
                    MapToJson(fxRatesList, timestamp),
                    MapToJson(cfdQuotesList, timestamp),
                    SnapshotStatus.Final);
            }
        }

        private void ApplyFxRate(ImmutableArray<Position> positionsProvider, 
            ImmutableArray<MarginTradingAccount> accountsProvider, 
            decimal closePrice, 
            string instrument)
        {
            var positions = positionsProvider.Where(p => p.FxAssetPairId == instrument);

            var positionsByAccounts = positions
                .GroupBy(p => p.AccountId)
                .ToDictionary(p => p.Key, p => p.ToArray());
            
            foreach (var accountPositions in positionsByAccounts)
            {
                foreach (var position in accountPositions.Value)
                {
                    var fxPrice = _cfdCalculatorService.GetPrice(closePrice, 
                        closePrice, 
                        position.FxToAssetPairDirection,
                        position.Volume * (position.ClosePrice - position.OpenPrice) > 0);

                    position.UpdateCloseFxPriceWithoutAccountUpdate(fxPrice);
                }
                
                var snapshotAccount = accountsProvider.SingleOrDefault(a => a.Id == accountPositions.Key);

                if (snapshotAccount == null)
                {
                    _log.WriteWarning(nameof(ApplyFxRate), null, $"Couldn't find account with id [{accountPositions.Key}] to apply fx rate update");
                    continue;
                }
                
                snapshotAccount.CacheNeedsToBeUpdated();
            }
        }

        private void ApplyCfdQuote(ImmutableArray<Position> positionsProvider, 
            ImmutableArray<Order> ordersProvider, 
            ImmutableArray<MarginTradingAccount> accountsProvider,
            decimal closePrice, 
            string instrument)
        {
            if (closePrice == 0)
                return;
            
            var positions = positionsProvider.Where(p => p.AssetPairId == instrument);
            
            var positionsByAccounts = positions
                .GroupBy(p => p.AccountId)
                .ToDictionary(p => p.Key, p => p.ToArray());

            foreach (var accountPositions in positionsByAccounts)
            {
                foreach (var position in accountPositions.Value)
                {
                    position.UpdateClosePriceWithoutAccountUpdate(closePrice);
                    
                    // update trailing stops
                    foreach (var relatedOrderId in position.GetTrailingStopOrderIds())
                    {
                        var relatedOrder = ordersProvider.SingleOrDefault(o => o.Id == relatedOrderId);
                        
                        if (relatedOrder == null)
                            continue;

                        var oldPrice = relatedOrder.Price;

                        relatedOrder.UpdateTrailingStopWithClosePrice(position.ClosePrice,() => _dateService.Now());

                        if (oldPrice != relatedOrder.Price)
                        {
                            _log.WriteInfoAsync(nameof(FinalSnapshotCalculator), nameof(ApplyCfdQuote),
                                $"Price for trailing stop order {relatedOrder.Id} changed. " +
                                $"Old price: {oldPrice}. " +
                                $"New price: {relatedOrder.Price}");   
                        }
                    }
                }
                
                var snapshotAccount = accountsProvider.SingleOrDefault(a => a.Id == accountPositions.Key);

                if (snapshotAccount == null)
                {
                    _log.WriteWarning(nameof(ApplyFxRate), null, $"Couldn't find account with id [{accountPositions.Key}] to apply cfd quote update");
                    continue;
                }
                
                snapshotAccount.CacheNeedsToBeUpdated();
            }
        }

        private static string MapToFinalJson(IList<Order> orders, IOrderReader reader) => 
            orders.Select(o => o.ConvertToSnapshotContract(reader)).ToJson();

        private static string MapToFinalJson(IList<Position> positions, IOrderReader reader) =>
            positions.Select(p => p.ConvertToSnapshotContract(reader)).ToJson();

        private static string MapToFinalJson(IList<MarginTradingAccount> accounts) =>
            accounts.Select(a => a.ConvertToSnapshotContract()).ToJson();

        private static string MapToJson(IList<ClosingFxRate> fxRates, DateTime timestamp) =>
            fxRates.Select(r => r.ToContract(timestamp)).ToJson();

        private static string MapToJson(IList<ClosingAssetPrice> cfdQuotes, DateTime timestamp) =>
            cfdQuotes.Select(q => q.ToContract(timestamp)).ToJson();
    }
}