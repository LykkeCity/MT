using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IQuoteHistoryRepository
	{
		Task<decimal?> GetClosestQuoteAsync(string instrument, OrderDirection direction, long ticks);
	}
}
