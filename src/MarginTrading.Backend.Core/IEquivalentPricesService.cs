using MarginTrading.Backend.Core.Orders;

namespace MarginTrading.Backend.Core
{
	public interface IEquivalentPricesService
	{
		void EnrichOpeningOrder(Position order);
		void EnrichClosingOrder(Position order);
	}
}