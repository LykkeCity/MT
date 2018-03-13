using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core.TradingConditions;

namespace MarginTrading.Backend.Services.TradingConditions
{
    public class TradingConditionsCacheService : ITradingConditionsCacheService
    {
        private ConcurrentDictionary<string, ITradingCondition> _tradingConditions =
            new ConcurrentDictionary<string, ITradingCondition>();
        
        public List<ITradingCondition> GetAllTradingConditions()
        {
            return _tradingConditions.Values.ToList();
        }

        public bool IsTradingConditionExists(string tradingConditionId)
        {
            return _tradingConditions.ContainsKey(tradingConditionId);
        }
        
        public ITradingCondition GetTradingCondition(string tradingConditionId)
        {
            if (!_tradingConditions.TryGetValue(tradingConditionId, out var result))
            {
                throw new Exception($"Tading condition with ID {tradingConditionId} was not found");
            }

            return result;
        }

        internal void InitTradingConditionsCache(List<ITradingCondition> tradingConditions)
        {
            _tradingConditions =
                new ConcurrentDictionary<string, ITradingCondition>(tradingConditions.ToDictionary(c => c.Id));
        }
        
        internal void AddOrUpdateTradingCondition(ITradingCondition tradingCondition)
        {
            _tradingConditions.AddOrUpdate(tradingCondition.Id, tradingCondition, (s, condition) => tradingCondition);
        }
    }
}
