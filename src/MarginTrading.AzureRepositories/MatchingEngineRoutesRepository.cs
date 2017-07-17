using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using MarginTrading.Core;
using Microsoft.WindowsAzure.Storage.Table;

namespace MarginTrading.AzureRepositories
{
    public class MatchingEngineRouteEntity : TableEntity, IMatchingEngineRoute
    {
        public string Id { get; set; }
        public int Rank { get; set; }
        public string TradingConditionId { get; set; }
        public string ClientId { get; set; }
        public string Instrument { get; set; }
        public string Type { get; set; }
        OrderDirection? IMatchingEngineRoute.Type => Type?.ParseEnum(OrderDirection.Buy);
        public string MatchingEngineId { get; set; }
      

        public static class Route
        {
            public static string GeneratePartitionKey()
            {
                return "Rule";
            }

            public static string GenerateRowKey(string id)
            {
                return id;
            }

            public static MatchingEngineRouteEntity Create(IMatchingEngineRoute route)
            {
                return new MatchingEngineRouteEntity
                {
                    PartitionKey = GeneratePartitionKey(),
                    RowKey = GenerateRowKey(route.Id),
                    Id = route.Id,
                    Rank = route.Rank,
                    TradingConditionId = route.TradingConditionId,
                    Instrument = route.Instrument,
                    Type = route.Type?.ToString(),
                    MatchingEngineId = route.MatchingEngineId,
                    ClientId = route.ClientId
                };
            }
        }
    }

    public class MatchingEngineRoutesRepository : IMatchingEngineRoutesRepository
    {
        private readonly INoSQLTableStorage<MatchingEngineRouteEntity> _tableStorage;

        public MatchingEngineRoutesRepository(INoSQLTableStorage<MatchingEngineRouteEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddOrReplaceRouteAsync(IMatchingEngineRoute route)
        {
            await _tableStorage.InsertOrReplaceAsync(MatchingEngineRouteEntity.Route.Create(route));
        }

        public async Task DeleteRouteAsync(string id)
        {
            await _tableStorage.DeleteIfExistAsync(MatchingEngineRouteEntity.Route.GeneratePartitionKey(), MatchingEngineRouteEntity.Route.GenerateRowKey(id));
        }

        public async Task<IEnumerable<IMatchingEngineRoute>> GetAllRoutesAsync()
        {
            var entities = await _tableStorage.GetDataAsync(MatchingEngineRouteEntity.Route.GeneratePartitionKey());
            return entities.Select(MatchingEngineRoute.Create);
        }
        
    }
}
