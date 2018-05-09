using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
	public interface IEquivalentPricesService
	{
		void EnrichOpeningOrder(Order order);
		void EnrichClosingOrder(Order order);
		decimal GetUsdEquivalent(decimal amount, string baseAsset, string legalEntity);
	}
}