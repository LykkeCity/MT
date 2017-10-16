using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices.Implemetation;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class PrimaryExchangeService : IPrimaryExchangeService
    {
        private readonly ReadWriteLockedDictionary<string, string> _primaryExchanges =
            new ReadWriteLockedDictionary<string, string>();

        private readonly IAlertService _alertService;
        private readonly IHedgingPriorityService _hedgingPriorityService;
        private readonly IPriceCalcSettingsService _priceCalcSettingsService;

        public PrimaryExchangeService(IAlertService alertService, IHedgingPriorityService hedgingPriorityService, IPriceCalcSettingsService priceCalcSettingsService)
        {
            _alertService = alertService;
            _hedgingPriorityService = hedgingPriorityService;
            _priceCalcSettingsService = priceCalcSettingsService;
        }

        public string GetPrimaryExchange(string assetPairId, ImmutableDictionary<string, ExchangeErrorState> exchanges)
        {
            if (!_priceCalcSettingsService.IsStepEnabled(OrderbookGeneratorStepEnum.ChoosePrimary, assetPairId))
            {
                return _priceCalcSettingsService.GetPresetPrimaryExchange(assetPairId);
            }

            var primaryExchange = _primaryExchanges.GetOrDefault(assetPairId);

            void AlertRiskOfficer(string message)
            {
                _alertService.AlertRiskOfficer(message, new { primaryExchange, exchanges });
            }

            void SwitchPrimaryExchange()
            {
                var exchangeWithQuality = ChooseBackupExchange(assetPairId, exchanges);
                primaryExchange = exchangeWithQuality.Exchange;
                _primaryExchanges[assetPairId] = primaryExchange;
                _alertService.AlertPrimaryExchangeSwitched(assetPairId, primaryExchange, exchangeWithQuality.State, exchangeWithQuality.Preference);
            }

            if (primaryExchange == null)
            {
                SwitchPrimaryExchange();
                AlertRiskOfficer($"{primaryExchange} is has been chosen as an initial primary exchange for {assetPairId}.");
                return primaryExchange;
            }

            var primaryExchangeState = exchanges[primaryExchange];
            var originalPrimaryExchange = primaryExchange;
            int cycleCounter = 0;
            do
            {
                if (cycleCounter++ >= 2)
                {
                    // Guard - in case somehow an Outdated or Disabled exchange gets chosen second time - restrict endless loop
                    throw new InvalidOperationException("Unable to get primary exchange for assetPair " + assetPairId);
                }

                switch (primaryExchangeState)
                {
                    case ExchangeErrorState.None:
                        break;
                    case ExchangeErrorState.Outlier:
                        AlertRiskOfficer($"Primary exchange {primaryExchange} for {assetPairId} is an outlier. Skipping price update.");
                        return null;
                    case ExchangeErrorState.Outdated:
                    case ExchangeErrorState.Disabled:
                        SwitchPrimaryExchange();
                        AlertRiskOfficer(
                            $"Primary exchange {originalPrimaryExchange} for {assetPairId} is not usable any more " +
                            $"because it became {primaryExchangeState}.\r\n" +
                            $"A new primary exchange has been chosen: {primaryExchange}");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(primaryExchangeState), primaryExchangeState, null);
                }
            } while (originalPrimaryExchange != primaryExchange);

            return primaryExchange;
        }

        [Pure]
        private ExchangeQuality ChooseBackupExchange(string assetPairId, ImmutableDictionary<string, ExchangeErrorState> exchangesErrors)
        {
            var allHedgingPriorities = _hedgingPriorityService.Get(assetPairId)
                .Select(p => new ExchangeQuality(p.Key, p.Value, exchangesErrors.GetValueOrDefault(p.Key, ExchangeErrorState.Disabled)))
                .Where(p => p.State != ExchangeErrorState.Disabled)
                .ToLookup(t => t.State);

            var primary = allHedgingPriorities[ExchangeErrorState.None]
                .OrderByDescending(p => p.Preference)
                .FirstOrDefault();

            if (primary != null && primary.Preference > 0)
            {
                return primary;
            }

            _alertService.AlertStopNewTrades(assetPairId);

            foreach (var state in new [] { ExchangeErrorState.None, ExchangeErrorState.Outlier })
            {
                primary = allHedgingPriorities[state].OrderByDescending(p => p.Preference).FirstOrDefault();
                if (primary != null)
                {
                    return primary;
                }
            }

            throw new InvalidOperationException("Unable to choose backup exchange for assetPair " + assetPairId);
        }

        private class ExchangeQuality
        {
            public string Exchange { get; }
            public decimal Preference { get; }
            public ExchangeErrorState State { get; }

            public ExchangeQuality(string exchange, decimal preference, ExchangeErrorState state)
            {
                Exchange = exchange;
                Preference = preference;
                State = state;
            }
        }
    }
}
