using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.Core;

namespace MarginTrading.Services
{
    public class MatchingEngineRoutesCacheService : IMatchingEngineRoutesCacheService
    {
        private List<IMatchingEngineRoute> _routes = new List<IMatchingEngineRoute>();

        public IMatchingEngineRoute GetMatchingEngineRoute(string clientId, string tradingConditionId, string instrument, OrderDirection orderType)
        {
            // Find Specific Rule:
            var specificRule = (from r in _routes
                                 where r.ClientId == clientId
                                 && r.TradingConditionId == tradingConditionId
                                 && r.Instrument == instrument
                                 && r.Type == orderType
                                 orderby r.Rank descending
                                 select r).FirstOrDefault();

            // Find generic rules

            // Generic Rules
            var genericRules = from r in _routes
                           where (r.ClientId == "*" // Generic
                                    && (r.TradingConditionId == "*")
                                    && (r.Instrument == "*")
                                    && (r.Type == null)
                                    && (r.Asset == null)
                                )
                                ||
                                (r.ClientId == clientId // client + generics
                                    && (r.TradingConditionId == tradingConditionId || r.TradingConditionId == "*")
                                    && (r.Instrument == instrument || r.Instrument == "*")                                    
                                    && (r.Type == orderType || r.Type == null)
                                    && (r.Asset == null)
                                )
                                || (r.TradingConditionId == tradingConditionId  // tradingConditionId + generics
                                    && (r.ClientId == clientId || r.ClientId== "*")
                                    && (r.Instrument == instrument || r.Instrument == "*")                                    
                                    && (r.Type == orderType || r.Type == null)
                                    && (r.Asset == null)
                                )
                                || (r.Instrument == instrument  // instrument + generics
                                    && (r.ClientId == clientId || r.ClientId == "*")
                                    && (r.TradingConditionId == tradingConditionId || r.TradingConditionId == "*")                                    
                                    && (r.Type == orderType || r.Type == null)
                                    && (r.Asset == null)
                                )
                                || (r.Type == orderType     // orderType + generics
                                    && (r.ClientId == clientId || r.ClientId == "*")
                                    && (r.TradingConditionId == tradingConditionId || r.TradingConditionId == "*")
                                    && (r.Instrument == instrument || r.Instrument == "*")
                                    && (r.Asset == null)
                                )
                           select r;
            // Asset rules            
            var assetRules = from r in _routes
                             where r.Asset != null && (
                                (instrument.StartsWith(r.Asset) && r.AssetType == AssetType.Base)
                                || (instrument.EndsWith(r.Asset) && r.AssetType == AssetType.Quote)
                             )
                             select r;
            // Filter by wildcards
            var filteredAssetRules = assetRules.Where(r => 
                (r.ClientId == clientId || r.ClientId == "*")
                && (r.TradingConditionId == tradingConditionId || r.TradingConditionId == "*")
                && (r.Instrument == instrument || r.Instrument == "*")
                && (r.Type == orderType || r.Type == null)
                );
            if (filteredAssetRules.Count() > 0)
                genericRules = genericRules.Concat(filteredAssetRules);
                
            
            // Comparison 1
            // If there is 1 rule with higher rank than all others, return it.

            // First concat specific rule and generic rules to single array 
            // (distict since specific rule should already exist in generics).
            var allrules = genericRules;
            if (specificRule != null)
                allrules = allrules.Append(specificRule).Distinct();
            // Perform comparison
            var maxRanked = from a in allrules
                                 where a.Rank == allrules.Max(item => item.Rank)
                                 select a;
            if (maxRanked.Count() == 1)
                return maxRanked.First();

            // Comparison 2
            // If specific rule's rank is >= than all generic rule's ranks, return specific rule.
            if (specificRule != null && specificRule.Rank >= genericRules.Max(item => item.Rank))
                return specificRule;

            // Comparison 3
            // If there are equal ranks in rules return more specific rule (with less wildcards)
            if (maxRanked.Count() > 1)
            {
                var mostSpecific = from m in maxRanked
                                   where m.SpecificationLevel() == maxRanked.Max(item => item.SpecificationLevel())
                                   select m;
                // Return Most Specific
                if (mostSpecific.Count() == 1)
                    return mostSpecific.First();
                else
                {
                    // Rules With Same Specification Level (same number of wild cards), check priority
                    var highestPriority = from m in mostSpecific
                                          where m.SpecificationPriority() == mostSpecific.Max(item => item.SpecificationPriority())
                                          select m;

                    // Return Highest Priority
                    if (highestPriority.Count() == 1)
                        return highestPriority.First();
                }
            }
            // If nothing returned yet, something is wrong with rules (duplicate rules).
            throw new InvalidOperationException("Could not resolve rule");
         
        }


        public IMatchingEngineRoute GetRoute(string id)
        {
            return _routes.Where(item => item.Id == id)
                .OrderBy(item => item.Rank)
                .FirstOrDefault();
        }

        public IMatchingEngineRoute[] GetRoutes()
        {
            return _routes.OrderBy(item => item.Rank)
                .ToArray();
        }

        internal void InitCache(List<IMatchingEngineRoute> routes)
        {
            _routes = routes;
        }
    }
}
