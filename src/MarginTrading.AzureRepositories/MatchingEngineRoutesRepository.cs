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
        
        public static class GlobalRoute
        {
            public static string GeneratePartitionKey()
            {
                return "GlobalRule";
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
                    MatchingEngineId = route.MatchingEngineId
                };
            }
        }

        public static class LocalRoute
        {
            public static string GeneratePartitionKey()
            {
                return "LocalRule";
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
                    ClientId = route.ClientId,
                    Instrument = route.Instrument,
                    Type = route.Type?.ToString(),
                    MatchingEngineId = route.MatchingEngineId
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

        public async Task AddOrReplaceGlobalRouteAsync(IMatchingEngineRoute route)
        {
            await _tableStorage.InsertOrReplaceAsync(MatchingEngineRouteEntity.GlobalRoute.Create(route));
        }

        public async Task AddOrReplaceLocalRouteAsync(IMatchingEngineRoute route)
        {
            await _tableStorage.InsertOrReplaceAsync(MatchingEngineRouteEntity.LocalRoute.Create(route));
        }

        public async Task DeleteGlobalRouteAsync(string id)
        {
            await _tableStorage.DeleteIfExistAsync(MatchingEngineRouteEntity.GlobalRoute.GeneratePartitionKey(), MatchingEngineRouteEntity.GlobalRoute.GenerateRowKey(id));
        }

        public async Task DeleteLocalRouteAsync(string id)
        {
            await _tableStorage.DeleteIfExistAsync(MatchingEngineRouteEntity.LocalRoute.GeneratePartitionKey(), MatchingEngineRouteEntity.LocalRoute.GenerateRowKey(id));
        }

        public async Task<IEnumerable<IMatchingEngineRoute>> GetAllGlobalRoutesAsync()
        {
            var entities = await _tableStorage.GetDataAsync(MatchingEngineRouteEntity.GlobalRoute.GeneratePartitionKey());
            return entities.Select(MatchingEngineRoute.Create);
        }

        public async Task<IEnumerable<IMatchingEngineRoute>> GetAllLocalRoutesAsync()
        {
            var entities = await _tableStorage.GetDataAsync(MatchingEngineRouteEntity.LocalRoute.GeneratePartitionKey());
            return entities.Select(MatchingEngineRoute.Create);
        }
    }
}
