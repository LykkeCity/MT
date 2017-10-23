using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Messages;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class PrimaryExchangeService : IPrimaryExchangeService
    {
        private readonly ReadWriteLockedDictionary<string, string> _primaryExchanges =
            new ReadWriteLockedDictionary<string, string>();

        private readonly IAlertService _alertService;
        private readonly IHedgingPreferenceService _hedgingPreferenceService;
        private readonly IPriceCalcSettingsService _priceCalcSettingsService;
        private readonly IStopTradesService _stopTradesService;

        public PrimaryExchangeService(IAlertService alertService, IHedgingPreferenceService hedgingPreferenceService,
            IPriceCalcSettingsService priceCalcSettingsService, IStopTradesService stopTradesService)
        {
            _alertService = alertService;
            _hedgingPreferenceService = hedgingPreferenceService;
            _priceCalcSettingsService = priceCalcSettingsService;
            _stopTradesService = stopTradesService;
        }

        public string GetPrimaryExchange(string assetPairId, ImmutableDictionary<string, ExchangeErrorState> errors,
            DateTime now)
        {
            if (!_priceCalcSettingsService.IsStepEnabled(OrderbookGeneratorStepEnum.ChoosePrimary, assetPairId))
            {
                return _priceCalcSettingsService.GetPresetPrimaryExchange(assetPairId);
            }

            var hedgingPreferences = _hedgingPreferenceService.Get(assetPairId);
            var result = CheckPrimaryStatusAndSwitchIfNeeded(assetPairId, errors, hedgingPreferences);
            _stopTradesService.SetPrimaryOrderbookState(assetPairId, result, now,
                hedgingPreferences.GetValueOrDefault(result), errors[result]);
            return result;
        }

        [CanBeNull]
        private string CheckPrimaryStatusAndSwitchIfNeeded(string assetPairId,
            ImmutableDictionary<string, ExchangeErrorState> errors,
            ImmutableDictionary<string, decimal> hedgingPreferences)
        {
            var primaryExchange = _primaryExchanges.GetOrDefault(assetPairId);
            var originalPrimaryExchange = primaryExchange;

            if (primaryExchange == null)
            {
                SwitchPrimaryExchange();
                _alertService.AlertRiskOfficer(
                    $"{primaryExchange} is has been chosen as an initial primary exchange for {assetPairId}.");
                return primaryExchange;
            }

            void SwitchPrimaryExchange()
            {
                var (newPrimary, all) = ChooseBackupExchange(assetPairId, errors, hedgingPreferences);
                primaryExchange = newPrimary.Exchange;
                _primaryExchanges[assetPairId] = primaryExchange;
                _alertService.AlertPrimaryExchangeSwitched(
                    new PrimaryExchangeSwitchedMessage
                    {
                        AssetPairId = assetPairId,
                        AllExchangesStates = all,
                        NewPrimaryExchange = newPrimary,
                    });
            }

            var primaryExchangeErrorState = errors[primaryExchange];
            var primaryPreference = hedgingPreferences.GetValueOrDefault(primaryExchange);
            switch (primaryExchangeErrorState)
            {
                case ExchangeErrorState.None when primaryPreference > 0:
                    return primaryExchange;
                case ExchangeErrorState.Outlier when primaryPreference > 0:
                    _alertService.AlertRiskOfficer(
                        $"Primary exchange {primaryExchange} for {assetPairId} is an outlier. Skipping price update.");
                    return null;
                default:
                    SwitchPrimaryExchange();
                    _alertService.AlertRiskOfficer(
                        $"Primary exchange {originalPrimaryExchange} for {assetPairId} was changed.\r\n" +
                        $"It had error state {primaryExchangeErrorState} and hedging priority {primaryPreference}.\r\n" +
                        $"New primary exchange: {primaryExchange}");
                    return errors[primaryExchange] == ExchangeErrorState.Outlier
                        ? null
                        : primaryExchange;
            }
        }

        [Pure]
        private static (ExchangeQuality primary, ImmutableArray<ExchangeQuality> exchangeQualities)
            ChooseBackupExchange(string assetPairId, ImmutableDictionary<string, ExchangeErrorState> exchangesErrors,
                ImmutableDictionary<string, decimal> hedgingPriorities)
        {
            ExchangeQuality GetExchangeQuality(string exchange, decimal preference)
            {
                var orderbookReceived = exchangesErrors.TryGetValue(exchange, out var state);
                return new ExchangeQuality(exchange, preference, orderbookReceived ? state : (ExchangeErrorState?) null,
                    orderbookReceived);
            }

            var exchangeQualities = hedgingPriorities
                .Select(p => GetExchangeQuality(p.Key, p.Value))
                .ToImmutableArray();
            var allHedgingPriorities = exchangeQualities
                .Where(p => p.Error != null && p.Error != ExchangeErrorState.Disabled)
                // ReSharper disable once PossibleInvalidOperationException
                .ToLookup(t => t.Error.Value);

            var primary = allHedgingPriorities[ExchangeErrorState.None]
                .OrderByDescending(p => p.Preference)
                .FirstOrDefault();

            if (primary != null && primary.Preference > 0)
            {
                return (primary, exchangeQualities);
            }

            foreach (var state in new[] {ExchangeErrorState.None, ExchangeErrorState.Outlier})
            {
                primary = allHedgingPriorities[state].OrderByDescending(p => p.Preference).FirstOrDefault();
                if (primary != null)
                {
                    return (primary, exchangeQualities);
                }
            }

            // this may spam shortly after service start
            throw new InvalidOperationException("Unable to choose backup exchange for assetPair " + assetPairId);
        }
    }
}