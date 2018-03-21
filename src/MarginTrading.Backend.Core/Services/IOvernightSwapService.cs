using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
	public interface IOvernightSwapService
	{
		/// <summary>
		/// Scheduler entry point for overnight swaps calculation. Successfully calculated swaps are immediately charged.
		/// </summary>
		/// <returns></returns>
		Task CalculateAndChargeSwaps();

		/// <summary>
		/// Initialization point
		/// </summary>
		void Start();
	}
}