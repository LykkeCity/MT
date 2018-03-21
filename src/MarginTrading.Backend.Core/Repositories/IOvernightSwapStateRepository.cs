using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
	public interface IOvernightSwapStateRepository
	{
		Task AddOrReplaceAsync(IOvernightSwapState obj);
		Task<IEnumerable<IOvernightSwapState>> GetAsync();
		Task<IReadOnlyList<IOvernightSwapState>> GetAsync(string accountId, DateTime? from, DateTime? to);
	}
}