using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IQuoteHistoryRepository
	{
		Task<double?> GetClosestQuoteAsync(string instrument, OrderDirection direction, long ticks);
	}
}
