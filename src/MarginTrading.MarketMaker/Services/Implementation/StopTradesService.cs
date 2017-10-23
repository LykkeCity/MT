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
        private readonly ReadWriteLockedDictionary<(string, string), State> _lastStates =
            new ReadWriteLockedDictionary<(string, string), State>();

        private readonly ReadWriteLockedDictionary<string, bool> _stoppedTradesAssetPairs
            = new ReadWriteLockedDictionary<string, bool>();

        private readonly IAlertService _alertService;

        public StopTradesService(IAlertService alertService)
        {
            _alertService = alertService;
        }

        public void SetPrimaryOrderbookState(string assetPairId, string exchange, DateTime now, decimal hedgingPriority,
            ExchangeErrorState errorState)
        {
            var primaryState = new PrimaryState(exchange, errorState, hedgingPriority);
            _lastStates.AddOrUpdate((assetPairId, exchange),
                k => new State(null, now, primaryState: primaryState),
                (k, old) => new State(old, now, primaryState: primaryState));
        }

        public void SetFreshOrderbooksState(ImmutableDictionary<string, ExternalOrderbook> freshOrderbooks, DateTime now)
        {
            var freshOrderbooksState = new FreshOrderbooksState(freshOrderbooks.Keys);
            var first = freshOrderbooks.Values.First();
            _lastStates.AddOrUpdate((first.AssetPairId, first.ExchangeName),
                k => new State(null, now, freshOrderbooksState),
                (k, old) => new State(old, now, freshOrderbooksState));
        }

        public void FinishCycle(ExternalOrderbook primaryOrderbook, DateTime now)
        {
            if (!_lastStates.TryGetValue((primaryOrderbook.AssetPairId, primaryOrderbook.ExchangeName), out var state) ||
                now != state.Time)
            {
                return;
            }

            var isPrimaryOk = state.PrimaryState == null ||
                              state.PrimaryState.HedgingPriority > 0 &&
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
                        $"Primary exchange {state.PrimaryState.Name} has hedging priority {state.PrimaryState.HedgingPriority} and error state {state.PrimaryState.ErrorState}. ";
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

        private class PrimaryState
        {
            public string Name { get; }
            public ExchangeErrorState ErrorState { get; }
            public decimal HedgingPriority { get; }

            public PrimaryState(string name, ExchangeErrorState errorState, decimal hedgingPriority)
            {
                ErrorState = errorState;
                HedgingPriority = hedgingPriority;
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
            public State([CanBeNull] State old, DateTime time,
                FreshOrderbooksState freshOrderbooksState = null, PrimaryState primaryState = null)
            {
                Time = time;

                if (old?.Time == time)
                {
                    FreshOrderbooksState = freshOrderbooksState ?? old.FreshOrderbooksState;
                    PrimaryState = primaryState ?? old.PrimaryState;
                }
                else
                {
                    FreshOrderbooksState = freshOrderbooksState;
                    PrimaryState = primaryState;
                }
            }

            public DateTime Time { get; }

            [CanBeNull]
            public FreshOrderbooksState FreshOrderbooksState { get; }
            [CanBeNull]
            public PrimaryState PrimaryState { get; }
        }
    }
}