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
