using AzureStorage;
using MarginTrading.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AzureRepositories
{
	public class MarginTradingTradingOrderRepository : IMarginTradingTradingOrderRepository
	{
		private readonly INoSQLTableStorage<TradingOrderEntity> _tableStorage;

		public MarginTradingTradingOrderRepository(INoSQLTableStorage<TradingOrderEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}

		public async Task AddAsync(ITradingOrder order)
		{
			var entity = TradingOrderEntity.Create(order);
			await _tableStorage.InsertOrMergeAsync(entity);
		}

		public bool Any()
		{
			return _tableStorage.Any();
		}

		public async Task<IEnumerable<ITradingOrder>> GetOrdersAsync(DateTime? from = default(DateTime?), DateTime? to = default(DateTime?))
		{
			var result = await _tableStorage.GetDataAsync(x => (from != null ? x.Timestamp >= from : true) && (to != null ? x.Timestamp <= to : true));

			return result.Select(TradingOrderEntity.Restore);
		}
	}
}
