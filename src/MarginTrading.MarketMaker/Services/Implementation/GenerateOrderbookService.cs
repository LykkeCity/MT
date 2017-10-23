using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Autofac;
using Common;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Models;
using Trace = MarginTrading.MarketMaker.HelperServices.Implemetation.Trace;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    /// <summary>
    ///     Generates orderbooks from external exchanges
    /// </summary>
    /// <remarks>
    ///     https://lykkex.atlassian.net/wiki/spaces/MW/pages/84607035/Price+setting
    /// </remarks>
    public class GenerateOrderbookService : IStartable, IDisposable, IGenerateOrderbookService
    {
        private readonly IOrderbooksService _orderbooksService;
        private readonly IDisabledOrderbooksService _disabledOrderbooksService;
        private readonly IOutdatedOrderbooksService _outdatedOrderbooksService;
        private readonly IOutliersOrderbooksService _outliersOrderbooksService;
        private readonly IRepeatedProblemsOrderbooksService _repeatedProblemsOrderbooksService;
        private readonly IPriceCalcSettingsService _priceCalcSettingsService;
        private readonly IAlertService _alertService;
        private readonly IPrimaryExchangeService _primaryExchangeService;
        private readonly IArbitrageFreeSpreadService _arbitrageFreeSpreadService;
        private readonly IBestPricesService _bestPricesService;
        private readonly ILog _log;
        private readonly ITelemetryService _telemetryService;
        private readonly ITestingHelperService _testingHelperService;
        private readonly IStopTradesService _stopTradesService;


        public GenerateOrderbookService(
            IOrderbooksService orderbooksService,
            IDisabledOrderbooksService disabledOrderbooksService,
            IOutdatedOrderbooksService outdatedOrderbooksService,
            IOutliersOrderbooksService outliersOrderbooksService,
            IRepeatedProblemsOrderbooksService repeatedProblemsOrderbooksService,
            IPriceCalcSettingsService priceCalcSettingsService,
            IAlertService alertService,
            IPrimaryExchangeService primaryExchangeService,
            IArbitrageFreeSpreadService arbitrageFreeSpreadService,
            IBestPricesService bestPricesService,
            ILog log,
            ITelemetryService telemetryService,
            ITestingHelperService testingHelperService,
            IStopTradesService stopTradesService)
        {
            _orderbooksService = orderbooksService;
            _disabledOrderbooksService = disabledOrderbooksService;
            _outdatedOrderbooksService = outdatedOrderbooksService;
            _outliersOrderbooksService = outliersOrderbooksService;
            _repeatedProblemsOrderbooksService = repeatedProblemsOrderbooksService;
            _priceCalcSettingsService = priceCalcSettingsService;
            _alertService = alertService;
            _primaryExchangeService = primaryExchangeService;
            _arbitrageFreeSpreadService = arbitrageFreeSpreadService;
            _bestPricesService = bestPricesService;
            _log = log;
            _telemetryService = telemetryService;
            _testingHelperService = testingHelperService;
            _stopTradesService = stopTradesService;
        }

        public Orderbook OnNewOrderbook(ExternalOrderbook orderbook)
        {
            var watch = Stopwatch.StartNew();
            orderbook = _testingHelperService.ModifyOrderbookIfNeeded(orderbook);
            if (orderbook == null)
            {
                return null;
            }

            var assetPairId = orderbook.AssetPairId;
            var allOrderbooks = _orderbooksService.AddAndGetByAssetPair(orderbook);
            var now = orderbook.LastUpdatedTime;
            var (exchangesErrors, validOrderbooks) = MarkExchangesErrors(assetPairId, allOrderbooks, now);
            var primaryExchange = _primaryExchangeService.GetPrimaryExchange(assetPairId, exchangesErrors, now);
            if (primaryExchange == null || primaryExchange != orderbook.ExchangeName)
            {
                return null;
            }

            if (!allOrderbooks.TryGetValue(primaryExchange, out var primaryOrderbook))
            {
                _log.WriteWarningAsync(nameof(GenerateOrderbookService), null,
                    $"{primaryExchange} not found in allOrderbooks ({allOrderbooks.Keys.ToJson()})");
                return null;
            }

            _stopTradesService.FinishCycle(primaryOrderbook, now);
            var result = Transform(primaryOrderbook, validOrderbooks);
            LogCycle(orderbook, watch, primaryExchange);
            return result;
        }

        public void Start()
        {
            _alertService.AlertStarted();
        }

        public void Dispose()
        {
            _alertService.AlertStopping().GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Detects exchanges errors and disables thm if they get repeated
        /// </summary>
        private (ImmutableDictionary<string, ExchangeErrorState>, ImmutableDictionary<string, ExternalOrderbook>)
            MarkExchangesErrors(string assetPairId, ImmutableDictionary<string, ExternalOrderbook> allOrderbooks, DateTime now)
        {
            var disabledExchanges = _disabledOrderbooksService.GetDisabledExchanges(assetPairId);
            var enabledOrderbooks = allOrderbooks.RemoveRange(disabledExchanges);
            var (outdatedExchanges, freshOrderbooks) = FindOutdated(assetPairId, enabledOrderbooks, now);
            var (outliersExchanges, validOrderbooks) = FindOutliers(assetPairId, freshOrderbooks, now);

            var repeatedProblemsExchanges = GetRepeatedProblemsExchanges(assetPairId, enabledOrderbooks,
                outdatedExchanges, outliersExchanges, now);
            _disabledOrderbooksService.Disable(assetPairId, repeatedProblemsExchanges);

            var exchangesErrors = ImmutableDictionary.CreateBuilder<string, ExchangeErrorState>()
                .SetValueForKeys(disabledExchanges, ExchangeErrorState.Disabled)
                .SetValueForKeys(outdatedExchanges, ExchangeErrorState.Outdated)
                .SetValueForKeys(outliersExchanges, ExchangeErrorState.Outlier)
                .SetValueForKeys(validOrderbooks.Keys, ExchangeErrorState.None)
                .SetValueForKeys(repeatedProblemsExchanges, ExchangeErrorState.Disabled)
                .ToImmutable();

            return (exchangesErrors, validOrderbooks);
        }

        /// <summary>
        ///     Applies arbitrage-free spread to the orderbook
        /// </summary>
        [CanBeNull]
        private Orderbook Transform(
            ExternalOrderbook primaryOrderbook,
            ImmutableDictionary<string, ExternalOrderbook> validOrderbooks)
        {
            if (!_priceCalcSettingsService.IsStepEnabled(OrderbookGeneratorStepEnum.Transform,
                primaryOrderbook.AssetPairId))
            {
                return primaryOrderbook;
            }

            var bestPrices =
                validOrderbooks.Values.ToDictionary(o => o.ExchangeName,
                    orderbook => _bestPricesService.Calc(orderbook));
            return _arbitrageFreeSpreadService.Transform(primaryOrderbook, bestPrices);
        }

        /// <summary>
        ///     Detects exchanges with repeated problems
        /// </summary>
        private ImmutableHashSet<string> GetRepeatedProblemsExchanges(string assetPairId,
            ImmutableDictionary<string, ExternalOrderbook> orderbooksByExchanges,
            ImmutableHashSet<string> outdatedExchanges, ImmutableHashSet<string> outliersExchanges,
            DateTime now)
        {
            if (!_priceCalcSettingsService.IsStepEnabled(OrderbookGeneratorStepEnum.FindRepeatedProblems, assetPairId))
            {
                return ImmutableHashSet<string>.Empty;
            }

            return orderbooksByExchanges.Values
                .Where(o => _repeatedProblemsOrderbooksService.IsRepeatedProblemsOrderbook(o,
                    outdatedExchanges.Contains(o.ExchangeName), outliersExchanges.Contains(o.ExchangeName), now))
                .Select(o => o.ExchangeName).ToImmutableHashSet();
        }


        /// <summary>
        ///     Detects outlier exchanges
        /// </summary>
        private (ImmutableHashSet<string>, ImmutableDictionary<string, ExternalOrderbook>) FindOutliers(
            string assetPairId, ImmutableDictionary<string, ExternalOrderbook> freshOrderbooks, DateTime now)
        {
            if (!_priceCalcSettingsService.IsStepEnabled(OrderbookGeneratorStepEnum.FindOutliers, assetPairId))
            {
                return (ImmutableHashSet<string>.Empty, freshOrderbooks);
            }

            _stopTradesService.SetFreshOrderbooksState(freshOrderbooks, now);
            if (freshOrderbooks.Count < 3)
            {
                return (ImmutableHashSet<string>.Empty, freshOrderbooks);
            }

            var outliersExchanges = _outliersOrderbooksService.FindOutliers(assetPairId, freshOrderbooks)
                .Select(o => o.ExchangeName)
                .ToImmutableHashSet();
            var freshNotOutlierOrderbooks = freshOrderbooks.RemoveRange(outliersExchanges);
            return (outliersExchanges, freshNotOutlierOrderbooks);
        }


        /// <summary>
        ///     Detects outdates exchanges
        /// </summary>
        private (ImmutableHashSet<string>, ImmutableDictionary<string, ExternalOrderbook>)
            FindOutdated(string assetPairId, ImmutableDictionary<string, ExternalOrderbook> orderbooksByExchanges,
                DateTime now)
        {
            if (!_priceCalcSettingsService.IsStepEnabled(OrderbookGeneratorStepEnum.FindOutdated, assetPairId))
            {
                return (ImmutableHashSet<string>.Empty, orderbooksByExchanges);
            }

            var outdatedExchanges = orderbooksByExchanges.Values
                .Where(o => _outdatedOrderbooksService.IsOutdated(o, now)).Select(o => o.ExchangeName)
                .ToImmutableHashSet();
            var freshOrderbooks = orderbooksByExchanges.RemoveRange(outdatedExchanges);
            return (outdatedExchanges, freshOrderbooks);
        }

        private void LogCycle(ExternalOrderbook orderbook, Stopwatch watch, string primaryExchange)
        {
            var elapsedMilliseconds = watch.ElapsedMilliseconds;
            if (elapsedMilliseconds > 20)
            {
                _telemetryService.PublishEventMetrics(nameof(GenerateOrderbookService) + '.' + nameof(OnNewOrderbook),
                    null,
                    new Dictionary<string, double> {{"ProcessingTime", elapsedMilliseconds}},
                    new Dictionary<string, string>
                    {
                        {"AssetPairId", orderbook.AssetPairId},
                        {"Exchange", orderbook.ExchangeName},
                    });
            }
            Trace.Write(
                $"Processed {orderbook.AssetPairId} from {orderbook.ExchangeName}, primary: {primaryExchange}, time: {elapsedMilliseconds} ms");
        }
    }
}