using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
	public interface IOvernightSwapService
	{
		/// <summary>
		/// Scheduler entry point for overnight swaps calculation. Successfully calculated swaps are immediately charged.
		/// </summary>
		/// <returns></returns>
		void CalculateAndChargeSwaps();

		/// <summary>
		/// Fire at app start. Initialize cache from storage. Detect if calc was missed and invoke it if needed.
		/// </summary>
		void Start();
	}
}