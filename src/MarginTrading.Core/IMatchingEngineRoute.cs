using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMatchingEngineRoute
    {
        string Id { get; }
        int Rank { get; }
        string TradingConditionId { get; }
        string ClientId { get; }
        string Instrument { get; }
        OrderDirection? Type { get; }
        string MatchingEngineId { get; }
        string Asset { get; }
        AssetType? AssetType { get; }
    }

    public class MatchingEngineRoute : IMatchingEngineRoute
    {
        public string Id { get; set; }
        public int Rank { get; set; }
        public string TradingConditionId { get; set; }
        public string ClientId { get; set; }
        public string Instrument { get; set; }
        public OrderDirection? Type { get; set; }
        public string MatchingEngineId { get; set; }
        public string Asset { get; set; }
        public AssetType? AssetType { get; set; }

        public static IMatchingEngineRoute Create(IMatchingEngineRoute src)
        {
            return new MatchingEngineRoute
            {
                Id = src.Id,
                Rank = src.Rank,
                TradingConditionId = src.TradingConditionId,
                ClientId = src.ClientId,
                Instrument = src.Instrument,
                Type = src.Type,
                MatchingEngineId = src.MatchingEngineId,
                Asset = src.Asset,
                AssetType = src.AssetType
            };
        }
    }

    public interface IMatchingEngineRoutesRepository
    {
        Task AddOrReplaceRouteAsync(IMatchingEngineRoute route);
        Task DeleteRouteAsync(string id);
        Task<IEnumerable<IMatchingEngineRoute>> GetAllRoutesAsync();
    }

    public enum AssetType
    {
        Base,
        Quote
    }

    public static class MatchingEngineRouteExtensions
    {
        public static int SpecificationLevel(this IMatchingEngineRoute route)
        {
            int specLevel = 0;            
            specLevel += route.Instrument == "*" ? 0 : 1;
            specLevel += route.Type == null ? 0 : 1;
            specLevel += route.TradingConditionId == "*" ? 0 : 1;
            specLevel += route.ClientId == "*" ? 0 : 1;
            return specLevel;
        }
        
        public static int SpecificationPriority(this IMatchingEngineRoute route)
        {
            //matching field or wildcard in such priority:
            //instrument, type, trading condition, client.
            //Flag based 1+2+4+8 >> 1 with Instrument(8) is higher than 1 with Type(4), TradingConditionId(2), ClientId(1) = 7

            int priority = 0;            
            priority += route.Instrument == "*" ? 0 : 8;
            priority += route.Type == null ? 0 : 4;
            priority += route.TradingConditionId == "*" ? 0 : 2;
            priority += route.ClientId == "*" ? 0 : 1;
            return priority;
        }
    }
}
