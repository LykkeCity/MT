using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using MarginTrading.Core;
using MarginTrading.Core.MatchingEngines;
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
        public string Asset { get; set; }

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
                ClientId = route.ClientId,
                Asset = route.Asset
            };
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
            await _tableStorage.InsertOrReplaceAsync(MatchingEngineRouteEntity.Create(route));
        }

        public async Task DeleteRouteAsync(string id)
        {
            await _tableStorage.DeleteIfExistAsync(MatchingEngineRouteEntity.GeneratePartitionKey(), MatchingEngineRouteEntity.GenerateRowKey(id));
        }

        public async Task<IEnumerable<IMatchingEngineRoute>> GetAllRoutesAsync()
        {
            return await _tableStorage.GetDataAsync(MatchingEngineRouteEntity.GeneratePartitionKey());
        }

        public async Task<IMatchingEngineRoute> GetRouteByIdAsync(string id)
        {
            return await _tableStorage.GetDataAsync(MatchingEngineRouteEntity.GeneratePartitionKey(), MatchingEngineRouteEntity.GenerateRowKey(id));
        }
    }
}
