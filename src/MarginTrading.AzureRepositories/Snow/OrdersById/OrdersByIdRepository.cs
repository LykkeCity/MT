using System.Threading.Tasks;
using AzureStorage;

namespace MarginTrading.AzureRepositories.Snow.OrdersById
{
    internal class OrdersByIdRepository : IOrdersByIdRepository
    {
        private readonly INoSQLTableStorage<OrderByIdEntity> _tableStorage;

        public OrdersByIdRepository(INoSQLTableStorage<OrderByIdEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task TryInsertAsync(IOrderById order)
        {
            var entity = OrderByIdEntity.Create(order);
            // ReSharper disable once RedundantArgumentDefaultValue
            return _tableStorage.TryInsertAsync(entity);
        }

        public async Task<IOrderById> GetAsync(string id)
        {
            return await _tableStorage.GetDataAsync(OrderByIdEntity.GeneratePartitionKey(id),
                OrderByIdEntity.GenerateRowKey());
        }
    }
}