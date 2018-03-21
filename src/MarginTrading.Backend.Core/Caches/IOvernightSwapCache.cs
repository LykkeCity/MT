using System.Collections.Generic;

namespace MarginTrading.Backend.Core
{
	public interface IOvernightSwapCache
	{
		OvernightSwapCalculation Get(string key);
		bool TryGet(string key, out OvernightSwapCalculation item);
		IReadOnlyList<OvernightSwapCalculation> GetAll();
		bool TryAdd(OvernightSwapCalculation item);
		bool AddOrReplace(OvernightSwapCalculation item);
		void SetAll(IEnumerable<OvernightSwapCalculation> items);
		void Clear(string key);
		void ClearAll();
		void Initialize(IEnumerable<OvernightSwapCalculation> items);
	}
}