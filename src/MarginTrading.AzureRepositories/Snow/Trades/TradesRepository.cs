using System.Threading.Tasks;
using AzureStorage;

namespace MarginTrading.AzureRepositories.Snow.Trades
{
    public class TradesRepository : ITradesRepository
    {
        private readonly INoSQLTableStorage<TradeEntity> _tableStorage;

        public TradesRepository(INoSQLTableStorage<TradeEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        // todo: use internal models instead of entity in the repo api
        public Task<TradeEntity> GetAsync(string id)
        {
            return _tableStorage.GetDataAsync(TradeEntity.GetPartitionKey(id), TradeEntity.GetRowKey());
        }

        // todo: use internal models instead of entity in the repo api
        public Task UpsertAsync(TradeEntity entity)
        {
            return _tableStorage.InsertOrReplaceAsync(entity);
        }
    }
}