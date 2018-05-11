using System.Threading.Tasks;
using AzureStorage;

namespace MarginTrading.AzureRepositories.Snow.Trades
{
    internal class TradesRepository : ITradesRepository
    {
        private readonly INoSQLTableStorage<TradeEntity> _tableStorage;

        public TradesRepository(INoSQLTableStorage<TradeEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        // todo: use internal models instead of entity in the repo api
        public async Task<ITrade> GetAsync(string id)
        {
            return await _tableStorage.GetDataAsync(TradeEntity.GetPartitionKey(id), TradeEntity.GetRowKey());
        }

        // todo: use internal models instead of entity in the repo api
        public Task UpsertAsync(ITrade trade)
        {
            var entity = TradeEntity.Create(trade);
            return _tableStorage.InsertOrReplaceAsync(entity);
        }
    }
}