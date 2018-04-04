using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
	public interface IOvernightSwapCache
	{
		bool TryGet(string key, out OvernightSwapCalculation item);
		IReadOnlyList<OvernightSwapCalculation> GetAll();
		bool AddOrReplace(OvernightSwapCalculation item);
		void Remove(OvernightSwapCalculation item);
		void ClearAll();
		void Initialize(IEnumerable<OvernightSwapCalculation> items);
	}
}