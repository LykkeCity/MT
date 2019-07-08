// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public interface ITradingConditionsCacheService
    {
        List<ITradingCondition> GetAllTradingConditions();
        ITradingCondition GetTradingCondition(string tradingConditionId);
        bool IsTradingConditionExists(string tradingConditionId);
    }
}
