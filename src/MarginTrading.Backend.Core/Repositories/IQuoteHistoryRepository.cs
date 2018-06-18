using System.Threading.Tasks;
using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
	public interface IQuoteHistoryRepository
	{
		Task<decimal?> GetClosestQuoteAsync(string instrument, OrderDirection direction, long ticks);
	}
}
