using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Common;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.HelperServices.Implemetation;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services.Implementation
{
    public class StopTradesService : IStopTradesService
    {
        private readonly ReadWriteLockedDictionary<string, ImmutableSortedDictionary<DateTime, State>> _lastStates =
            new ReadWriteLockedDictionary<string, ImmutableSortedDictionary<DateTime, State>>();

        private readonly ReadWriteLockedDictionary<string, bool> _stoppedTradesAssetPairs
            = new ReadWriteLockedDictionary<string, bool>();

        private readonly IAlertService _alertService;

        public StopTradesService(IAlertService alertService)
        {
            _alertService = alertService;
        }

        public void SetPrimaryOrderbookState(string assetPairId, string exchange, DateTime now,
            decimal hedgingPreference, ExchangeErrorState? errorState)
        {
            var state = new State(primaryState: new PrimaryState(exchange, errorState, hedgingPreference));
            AddOrUpdateState(assetPairId, now, state);
        }

        public void SetFreshOrderbooksState(string assetPairId, ImmutableDictionary<string, ExternalOrderbook> freshOrderbooks,
            DateTime now)
        {
            var state = new State(freshOrderbooksState: new FreshOrderbooksState(freshOrderbooks.Keys));
            AddOrUpdateState(assetPairId, now, state);
        }

        public void FinishCycle(ExternalOrderbook primaryOrderbook, DateTime now)
        {
            if (!_lastStates.TryGetValue(primaryOrderbook.AssetPairId, out var dict) ||
                !dict.TryGetValue(now, out var state))
            {
                return;
            }

            var isPrimaryOk = state.PrimaryState == null ||
                              state.PrimaryState.HedgingPreference > 0 &&
                              (state.PrimaryState.ErrorState == ExchangeErrorState.None ||
                               state.PrimaryState.ErrorState == ExchangeErrorState.Outlier);

            var isFreshOk = state.FreshOrderbooksState == null ||
                            state.FreshOrderbooksState.FreshOrderbooksKeys.Length > 2;

            var stop = !isPrimaryOk || !isFreshOk;
            var wasStopped = false;
            _stoppedTradesAssetPairs.AddOrUpdate(primaryOrderbook.AssetPairId, k => stop, (k, old) =>
            {
                wasStopped = old;
                return stop;
            });

            if (stop != wasStopped)
            {
                string reason = null;
                if (state.PrimaryState != null)
                {
                    reason +=
                        $"Primary exchange \"{state.PrimaryState.Name}\" has hedging preference \"{state.PrimaryState.HedgingPreference}\" and error state \"{state.PrimaryState.ErrorState}\". ";
                }

                if (state.FreshOrderbooksState != null)
                {
                    reason +=
                        $"Found {state.FreshOrderbooksState.FreshOrderbooksKeys.Length} fresh orderbooks: {state.FreshOrderbooksState.FreshOrderbooksKeys.ToJson()}. ";
                }

                reason = reason ?? "Everything is ok";
                _alertService.StopOrAllowNewTrades(primaryOrderbook.AssetPairId, reason, stop);
            }
        }
        private void AddOrUpdateState(string assetPairId, DateTime now, State state)
        {
            _lastStates.AddOrUpdate(assetPairId,
                k => ImmutableSortedDictionary.Create<DateTime, State>().Add(now, state),
                (k, old) => AddOrUpdateStatesDictAndCleanOld(old, state, now));
        }

        private static ImmutableSortedDictionary<DateTime, State> AddOrUpdateStatesDictAndCleanOld(
            ImmutableSortedDictionary<DateTime, State> states, State newState,
            DateTime now)
        {
            var minEventTime = now - TimeSpan.FromMinutes(1);
            var old = states.Keys.SkipWhile(e => e < minEventTime);
            states = states.RemoveRange(old);
            if (states.TryGetValue(now, out var oldState))
                newState = new State(oldState, newState);

            return states.SetItem(now, newState);
        }

        private class PrimaryState
        {
            public string Name { get; }
            public ExchangeErrorState? ErrorState { get; }
            public decimal HedgingPreference { get; }

            public PrimaryState(string name, ExchangeErrorState? errorState, decimal hedgingPreference)
            {
                ErrorState = errorState;
                HedgingPreference = hedgingPreference;
                Name = name;
            }
        }

        private class FreshOrderbooksState
        {
            public ImmutableArray<string> FreshOrderbooksKeys { get; }

            public FreshOrderbooksState(IEnumerable<string> freshOrderbooksKeys)
            {
                FreshOrderbooksKeys = freshOrderbooksKeys.ToImmutableArray();
            }
        }

        private class State
        {
            [CanBeNull]
            public FreshOrderbooksState FreshOrderbooksState { get; }

            [CanBeNull]
            public PrimaryState PrimaryState { get; }

            public State(FreshOrderbooksState freshOrderbooksState = null, PrimaryState primaryState = null)
            {
                FreshOrderbooksState = freshOrderbooksState;
                PrimaryState = primaryState;
            }

            public State(State oldState, State newState)
            {
                FreshOrderbooksState = newState.FreshOrderbooksState ?? oldState.FreshOrderbooksState;
                PrimaryState = newState.PrimaryState ?? oldState.PrimaryState;
            }
        }
    }
}