using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IStopTradesService
    {
        void SetPrimaryOrderbookState(string assetPairId, string exchange, DateTime now, decimal hedgingPreference, ExchangeErrorState? errorState);

        void SetFreshOrderbooksState(string assetPairId, ImmutableDictionary<string, ExternalOrderbook> freshOrderbooks, DateTime now);

        void FinishCycle(ExternalOrderbook primaryOrderbook, DateTime now);
    }
}