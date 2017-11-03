using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public class TradingConditionsCacheService : ITradingConditionsCacheService
    {
        private List<ITradingCondition> _tradingConditions = new List<ITradingCondition>();
        
        public List<ITradingCondition> GetAllTradingConditions()
        {
            return _tradingConditions;
        }

        public ITradingCondition GetTradingCondition(string tradingConditionId)
        {
            return _tradingConditions.FirstOrDefault(item => item.Id == tradingConditionId);
        }

        internal void InitTradingConditionsCache(List<ITradingCondition> tradingConditions)
        {
            _tradingConditions = tradingConditions;
        }
    }
}
