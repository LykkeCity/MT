using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class TradingConditionsCacheService : ITradingConditionsCacheService
    {
        private List<IMarginTradingCondition> _tradingConditions = new List<IMarginTradingCondition>();
        
        public List<IMarginTradingCondition> GetAllTradingConditions()
        {
            return _tradingConditions;
        }

        public IMarginTradingCondition GetTradingCondition(string tradingConditionId)
        {
            return _tradingConditions.FirstOrDefault(item => item.Id == tradingConditionId);
        }

        internal void InitTradingConditionsCache(List<IMarginTradingCondition> tradingConditions)
        {
            _tradingConditions = tradingConditions;
        }
    }
}
