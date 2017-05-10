using System.Collections.Generic;

namespace MarginTrading.Core
{
    public interface ITradingConditionsCacheService
    {
        List<IMarginTradingCondition> GetAllTradingConditions();
        IMarginTradingCondition GetTradingCondition(string tradingConditionId);
    }
}
