using System;
using System.Collections.Immutable;
using System.Linq;
using Common;
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

        public string GetPrimaryExchange(string assetPairId, ImmutableDictionary<string, ExchangeErrorState> errors)
        {
            if (!_priceCalcSettingsService.IsStepEnabled(OrderbookGeneratorStepEnum.ChoosePrimary, assetPairId))
            {
                return _priceCalcSettingsService.GetPresetPrimaryExchange(assetPairId);
            }

            var primaryExchange = _primaryExchanges.GetOrDefault(assetPairId);
            var hedgingPreferences = _hedgingPreferenceService.Get(assetPairId);
            var originalPrimaryExchange = primaryExchange;

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

            if (primaryExchange == null)
            {
                SwitchPrimaryExchange();
                _alertService.AlertRiskOfficer(
                    $"{primaryExchange} is has been chosen as an initial primary exchange for {assetPairId}.");
                return primaryExchange;
            }

            var primaryExchangeState = errors[primaryExchange];
            var primaryPreference = hedgingPreferences.GetValueOrDefault(primaryExchange);
            switch (primaryExchangeState)
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
                        $"It had error state {primaryExchangeState} and hedging priority {primaryPreference}.\r\n" +
                        $"New primary exchange: {primaryExchange}");
                    return errors[primaryExchange] == ExchangeErrorState.Outlier
                        ? null
                        : primaryExchange;
            }
        }

        [Pure]
        private (ExchangeQuality primary, ImmutableArray<ExchangeQuality> exchangeQualities)
            ChooseBackupExchange(string assetPairId, ImmutableDictionary<string, ExchangeErrorState> exchangesErrors,
                ImmutableDictionary<string, decimal> hedgingPriorities)
        {
            ExchangeQuality GetExchangeQuality(string exchange, decimal preference)
            {
                var orderbookReceived = exchangesErrors.TryGetValue(exchange, out var state);
                return new ExchangeQuality(exchange, preference, orderbookReceived ? state : (ExchangeErrorState?) null, orderbookReceived);
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
                if (exchangesErrors.Values.Count(e => e == ExchangeErrorState.None || e == ExchangeErrorState.Outdated) >= 3)
                {
                    // todo: make up a mechanism to enable trades, which takes in account all checks like this in the price cycle and decides in the end
                    // without it we cannot add new checks
                    _alertService.AlertAllowNewTrades(assetPairId, "Switched to a good exchange: " + primary.ToJson());
                }

                return (primary, exchangeQualities);
            }

            _alertService.AlertStopNewTrades(assetPairId, "Couldn't find a valid backup exchange");

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