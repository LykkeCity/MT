using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core.MatchingEngines
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
        string RiskSystemLimitType { get; set; }
        string RiskSystemMetricType { get; set; }
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
        public string RiskSystemLimitType { get; set; }
        public string RiskSystemMetricType { get; set; }

        public static MatchingEngineRoute Create(IMatchingEngineRoute src)
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
                RiskSystemLimitType = src.RiskSystemLimitType,
                RiskSystemMetricType = src.RiskSystemMetricType
            };
        }
    }

    public interface IMatchingEngineRoutesRepository
    {
        Task AddOrReplaceRouteAsync(IMatchingEngineRoute route);
        Task DeleteRouteAsync(string id);
        Task<IEnumerable<IMatchingEngineRoute>> GetAllRoutesAsync();
        Task<IMatchingEngineRoute> GetRouteByIdAsync(string id);
    }
}
