using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
	public interface IQuoteHistoryRepository
	{
		Task<decimal?> GetClosestQuoteAsync(string instrument, OrderDirection direction, long ticks);
	}
}
