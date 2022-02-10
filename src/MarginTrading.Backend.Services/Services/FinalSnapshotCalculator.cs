// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts.Prices;
using MarginTrading.Backend.Contracts.Snow.Prices;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
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
        private readonly ILog _log;
        private readonly IDraftSnapshotKeeper _draftSnapshotKeeper;

        public FinalSnapshotCalculator(ICfdCalculatorService cfdCalculatorService, ILog log, IDateService dateService, IDraftSnapshotKeeper draftSnapshotKeeper)
        {
            _cfdCalculatorService = cfdCalculatorService;
            _log = log;
            _dateService = dateService;
            _draftSnapshotKeeper = draftSnapshotKeeper;
        }
        
        /// <inheritdoc />
        public async Task<TradingEngineSnapshot> RunAsync(IEnumerable<ClosingFxRate> fxRates, IEnumerable<ClosingAssetPrice> cfdQuotes, string correlationId)
        {
            var fxRatesList = fxRates?.ToList();
            var cfdQuotesList = cfdQuotes?.ToList();
            
            if (fxRatesList == null || !fxRatesList.Any())
                throw new EmptyPriceUploadException();
            
            if (cfdQuotesList == null || !cfdQuotesList.Any())
                throw new EmptyPriceUploadException();
            
            var positions = _draftSnapshotKeeper.GetPositions();
            var accounts = (await _draftSnapshotKeeper.GetAccountsAsync()).ToImmutableArray();
            foreach (var closingFxRate in fxRatesList)
            {
                ApplyFxRate(positions, accounts, closingFxRate.ClosePrice, closingFxRate.AssetId);
            }
            
            var orders = _draftSnapshotKeeper.GetAllOrders();
            foreach (var closingAssetPrice in cfdQuotesList)
            {
                ApplyCfdQuote(positions, orders, accounts, closingAssetPrice.ClosePrice, closingAssetPrice.AssetId);
            }

            var quotesTimestamp = _dateService.Now();

            await _draftSnapshotKeeper.UpdateAsync(
                positions,
                orders,
                accounts,
                fxRatesList.Select(r => r.ToContract(quotesTimestamp)),
                cfdQuotesList.Select(q => q.ToContract(quotesTimestamp))
            );

            return new TradingEngineSnapshot(_draftSnapshotKeeper.TradingDay,
                correlationId,
                _draftSnapshotKeeper.Timestamp,
                MapToFinalJson(orders, _draftSnapshotKeeper),
                MapToFinalJson(positions, _draftSnapshotKeeper),
                MapToFinalJson(accounts),
                MapToJson(_draftSnapshotKeeper.FxPrices),
                MapToJson(_draftSnapshotKeeper.CfdQuotes),
                SnapshotStatus.Final);
        }

        private void ApplyFxRate(ImmutableArray<Position> positionsProvider, 
            ImmutableArray<MarginTradingAccount> accountsProvider, 
            decimal closePrice, 
            string instrument)
        {
            if (closePrice == 0)
                return;
            
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

        private static string MapToJson(IList<BestPriceContract> prices) =>
            prices.ToDictionary(p => p.Id, p => p).ToJson();
    }
}