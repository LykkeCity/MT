using System.Collections.Generic;
using System.Linq;
using MarginTrading.Backend.Core;
using MoreLinq;

namespace MarginTrading.Backend.Services.Caches
{
	public class OvernightSwapCache : IOvernightSwapCache
	{
		private Dictionary<string, OvernightSwapCalculation> _cache;

		private static readonly object LockObj = new object();

		public OvernightSwapCache()
		{
			ClearAll();
		}

		public bool TryGet(string key, out OvernightSwapCalculation item)
		{
			lock (LockObj)
			{
				return _cache.TryGetValue(key, out item);
			}
		}

		public IReadOnlyList<OvernightSwapCalculation> GetAll()
		{
			lock(LockObj)
			{
				return _cache.Values.ToList();
			}
		}

		public bool TryAdd(OvernightSwapCalculation item)
		{
			if (item == null)
				return false;
            
			lock (LockObj)
			{
				if (_cache.ContainsKey(item.Key))
					return false;
				
				_cache[item.Key] = item;
				return true;
			}
		}

		public bool AddOrReplace(OvernightSwapCalculation item)
		{
			if (item == null)
				return false;
			
			lock (LockObj)
			{
				_cache.Remove(item.Key);
				_cache[item.Key] = item;
			}

			return true;
		}

		private void SetAll(IEnumerable<OvernightSwapCalculation> items)
		{
			if (items == null)
				return;
            
			lock (LockObj)
			{
				items.Where(x => x != null).ForEach(x => _cache[x.Key] = x);
			}
		}

		public void Remove(OvernightSwapCalculation item)
		{
			if (item == null)
				return;
			
			lock (LockObj)
			{
				_cache.Remove(item.Key);
			}
		}

		public void ClearAll()
		{
			lock (LockObj)
			{
				_cache = new Dictionary<string, OvernightSwapCalculation>();
			}
		}

		public void Initialize(IEnumerable<OvernightSwapCalculation> items)
		{
			ClearAll();
			SetAll(items);
		}
	}
}