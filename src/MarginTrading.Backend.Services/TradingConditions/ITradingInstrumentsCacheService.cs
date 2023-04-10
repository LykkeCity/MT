// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface ITradingInstrumentsCacheService
    {
        void InitCache(IEnumerable<ITradingInstrument> tradingInstruments);

        void UpdateCache(ITradingInstrument tradingInstrument);
        
        [NotNull]
        ITradingInstrument GetTradingInstrument(string tradingConditionId, string instrument);

        (decimal MarginInit, decimal MarginMaintenance) GetMarginRates(ITradingInstrument tradingInstrument,
            bool isWarnCheck = false);

        void RemoveFromCache(string tradingConditionId, string instrument);
    }
}
