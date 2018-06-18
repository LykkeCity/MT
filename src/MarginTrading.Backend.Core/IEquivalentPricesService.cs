using MarginTrading.Backend.Core.Orders;
using MarginTrading.Backend.Core.Trading;

namespace MarginTrading.Backend.Core
{
	public interface IEquivalentPricesService
	{
		void EnrichOpeningOrder(Order order);
		void EnrichClosingOrder(Position order);
	}
}