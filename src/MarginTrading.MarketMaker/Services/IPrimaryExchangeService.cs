using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;
using MarginTrading.MarketMaker.Enums;
using MarginTrading.MarketMaker.Models;

namespace MarginTrading.MarketMaker.Services
{
    public interface IPrimaryExchangeService
    {
        [CanBeNull]
        string GetPrimaryExchange(string assetPairId, ImmutableDictionary<string, ExchangeErrorState> errors, DateTime now);

        [CanBeNull, Pure]
        string GetLastPrimaryExchange(string assetPairId);

        [Pure]
        IReadOnlyDictionary<string, ImmutableDictionary<string, ExchangeQuality>> GetQualities();
    }
}