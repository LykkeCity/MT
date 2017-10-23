using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IStopTradesService
    {
        void SetPrimaryOrderbookState(string assetPairId, string exchange, DateTime now, decimal hedgingPriority,
            ExchangeErrorState errorState);

        void SetFreshOrderbooksState(ImmutableDictionary<string, ExternalOrderbook> freshOrderbooks, DateTime now);

        void FinishCycle(ExternalOrderbook primaryOrderbook, DateTime now);
    }
}