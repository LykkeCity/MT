using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IRiskCalculationEngine
	{
		Task InitializeAsync();

		Task UpdateInternalStateAsync();

		Task ProcessTransactionAsync(IElementaryTransaction elementaryTransaction);
	}
}