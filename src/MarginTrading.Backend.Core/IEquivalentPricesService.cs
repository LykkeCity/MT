namespace MarginTrading.Backend.Core
{
	public interface IEquivalentPricesService
	{
		void EnrichOpeningOrder(Order order);
		void EnrichClosingOrder(Order order);
	}
}