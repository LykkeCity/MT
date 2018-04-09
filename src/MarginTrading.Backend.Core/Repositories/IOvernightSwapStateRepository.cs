using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
	public interface IOvernightSwapStateRepository
	{
		Task AddOrReplaceAsync(IOvernightSwapState obj);
		Task<IEnumerable<IOvernightSwapState>> GetAsync();
		Task DeleteAsync(IOvernightSwapState obj);
	}
}