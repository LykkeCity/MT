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

        public PrimaryExchangeService(IAlertService alertService, IHedgingPreferenceService hedgingPreferenceService,
            IPriceCalcSettingsService priceCalcSettingsService)
        {
            _alertService = alertService;
            _hedgingPreferenceService = hedgingPreferenceService;
            _priceCalcSettingsService = priceCalcSettingsService;
        }

        public string GetPrimaryExchange(string assetPairId, ImmutableDictionary<string, ExchangeErrorState> exchanges)
        {
            if (!_priceCalcSettingsService.IsStepEnabled(OrderbookGeneratorStepEnum.ChoosePrimary, assetPairId))
            {
                return _priceCalcSettingsService.GetPresetPrimaryExchange(assetPairId);
            }

            var primaryExchange = _primaryExchanges.GetOrDefault(assetPairId);
            var hedgingPreferences = _hedgingPreferenceService.Get(assetPairId);

            void SwitchPrimaryExchange()
            {
                var (newPrimary, all) = ChooseBackupExchange(assetPairId, exchanges, hedgingPreferences);
                primaryExchange = newPrimary.Exchange;
                _primaryExchanges[assetPairId] = primaryExchange;
                _alertService.AlertPrimaryExchangeSwitched(
                    new PrimaryExchangeSwitchedMessage(assetPairId, newPrimary, all));
            }

            if (primaryExchange == null)
            {
                SwitchPrimaryExchange();
                _alertService.AlertRiskOfficer(
                    $"{primaryExchange} is has been chosen as an initial primary exchange for {assetPairId}.");
                return primaryExchange;
            }

            var primaryExchangeState = exchanges[primaryExchange];
            var primaryPreference = hedgingPreferences.GetValueOrDefault(primaryExchange);
            var originalPrimaryExchange = primaryExchange;
            var cycleCounter = 0;
            do
            {
                if (cycleCounter++ >= 2)
                {
                    // Guard - in case somehow an Outdated or Disabled exchange gets chosen second time - restrict endless loop
                    throw new InvalidOperationException("Unable to get primary exchange for assetPair " + assetPairId);
                }

                switch (primaryExchangeState)
                {
                    case ExchangeErrorState.None when primaryPreference > 0:
                        break;
                    case ExchangeErrorState.Outlier when primaryPreference > 0:
                        _alertService.AlertRiskOfficer(
                            $"Primary exchange {primaryExchange} for {assetPairId} is an outlier. Skipping price update.");
                        return null;
                    default:
                        SwitchPrimaryExchange();
                        _alertService.AlertRiskOfficer(
                            $"Primary exchange {originalPrimaryExchange} for {assetPairId} is not usable any more " +
                            $"because it became {primaryExchangeState} with hedgind priority {primaryPreference}.\r\n" +
                            $"A new primary exchange has been chosen: {primaryExchange}");
                        break;
                }
            } while (originalPrimaryExchange != primaryExchange);

            return primaryExchange;
        }

        [Pure]
        private (ExchangeQuality primary, ImmutableArray<ExchangeQuality> exchangeQualities)
            ChooseBackupExchange(string assetPairId, ImmutableDictionary<string, ExchangeErrorState> exchangesErrors,
                ImmutableDictionary<string, decimal> hedgingPriorities)
        {
            ExchangeQuality GetExchangeQuality(string exchange, decimal preference)
            {
                var orderbookReceived = exchangesErrors.TryGetValue(exchange, out var state);
                return new ExchangeQuality(exchange, preference, state, orderbookReceived);
            }

            var exchangeQualities = hedgingPriorities
                .Select(p => GetExchangeQuality(p.Key, p.Value))
                .ToImmutableArray();
            var allHedgingPriorities = exchangeQualities
                .Where(p => p.State != null && p.State != ExchangeErrorState.Disabled)
                // ReSharper disable once PossibleInvalidOperationException
                .ToLookup(t => t.State.Value);

            var primary = allHedgingPriorities[ExchangeErrorState.None]
                .OrderByDescending(p => p.Preference)
                .FirstOrDefault();

            if (primary != null && primary.Preference > 0)
            {
                return (primary, exchangeQualities);
            }

            _alertService.AlertStopNewTrades(assetPairId);

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