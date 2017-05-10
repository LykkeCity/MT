using AzureStorage;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AzureRepositories
{
    public class MarginTradingOrderActionRepository : IMarginTradingOrderActionRepository
    {
        private readonly INoSQLTableStorage<OrderActionEntity> _tableStorage;

        public MarginTradingOrderActionRepository(INoSQLTableStorage<OrderActionEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task AddAsync(IOrderAction order)
        {
            var entity = OrderActionEntity.Create(order);
            await _tableStorage.InsertOrMergeAsync(entity);
        }

        public bool Any()
        {
            return _tableStorage.Any();
        }

        public async Task<IEnumerable<IOrderAction>> GetOrdersAsync(DateTime? from = default(DateTime?), DateTime? to = default(DateTime?))
        {
            var result = await _tableStorage.GetDataAsync(x => (from != null ? x.Timestamp >= from : true) && (to != null ? x.Timestamp <= to : true));

            return result.Select(OrderActionEntity.Restore);
        }
    }
}
