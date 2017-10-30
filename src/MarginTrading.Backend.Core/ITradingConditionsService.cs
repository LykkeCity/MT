using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
    public interface ITradingConditionsCacheService
    {
        List<IMarginTradingCondition> GetAllTradingConditions();
        IMarginTradingCondition GetTradingCondition(string tradingConditionId);
    }
}
