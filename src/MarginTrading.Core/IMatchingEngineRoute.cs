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
                MatchingEngineId = src.MatchingEngineId
            };
        }
    }

    public interface IMatchingEngineRoutesRepository
    {
        Task AddOrReplaceGlobalRouteAsync(IMatchingEngineRoute route);
        Task AddOrReplaceLocalRouteAsync(IMatchingEngineRoute route);
        Task DeleteGlobalRouteAsync(string id);
        Task DeleteLocalRouteAsync(string id);
        Task<IEnumerable<IMatchingEngineRoute>> GetAllGlobalRoutesAsync();
        Task<IEnumerable<IMatchingEngineRoute>> GetAllLocalRoutesAsync();
    }
}
