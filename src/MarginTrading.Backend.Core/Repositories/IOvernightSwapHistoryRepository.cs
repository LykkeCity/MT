using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core.Repositories
{
	public interface IOvernightSwapHistoryRepository
	{
		Task AddAsync(IOvernightSwapHistory obj);
		Task<IEnumerable<IOvernightSwapHistory>> GetAsync();
		Task<IReadOnlyList<IOvernightSwapHistory>> GetAsync(DateTime? @from, DateTime? to);
		Task<IReadOnlyList<IOvernightSwapHistory>> GetAsync(string accountId, DateTime? from, DateTime? to);
	}
}