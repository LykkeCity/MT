using AzureStorage;
using MarginTrading.Core;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace MarginTrading.AzureRepositories
{
	public class QuoteHistoryRepository : IQuoteHistoryRepository
	{
		private readonly INoSQLTableStorage<QuoteHistoryEntity> _tableStorage;

		public QuoteHistoryRepository(INoSQLTableStorage<QuoteHistoryEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}

		public async Task<double?> GetClosestQuoteAsync(string instrument, OrderDirection direction, long ticks)
		{
			try
			{
				var entity = await _tableStorage.GetDataAsync(QuoteHistoryEntity.GetPartitionKey(instrument, direction), QuoteHistoryEntity.GetRowKey(ticks));

				if (entity == null)
					return null;

				return GetClosestQuote(entity);
			}
			catch
			{
				return null;
			}
		}

		private double? GetClosestQuote(QuoteHistoryEntity entity)
		{
			entity.Items = JsonConvert.DeserializeObject<QuoteHistoryEntityItem[]>(entity.Part000);

			if (entity.Items == null || entity.Items.Length == 0)
			{
				return null;
			}

			var item = entity.Items.OrderByDescending(x => x.T).FirstOrDefault();

			if (item == null)
				return null;

			return item.P;
		}
	}
}
