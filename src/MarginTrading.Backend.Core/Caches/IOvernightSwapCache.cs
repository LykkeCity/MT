using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
	public interface IOvernightSwapCache
	{
		OvernightSwapCalculation Get(string key);
		IReadOnlyList<OvernightSwapCalculation> GetAll();
		bool TryAdd(OvernightSwapCalculation item);
		void SetAll(IEnumerable<OvernightSwapCalculation> items);
		void Clear(string key);
		void ClearAll();
		void Initialize(IEnumerable<OvernightSwapCalculation> items);
	}
}