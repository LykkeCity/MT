using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IMarginTradingPositionRepository
	{
		Task AddAsync(IPosition position);

		Task<IPosition> GetAsync(string clientId, string asset);

		Task UpdateAsync(IPosition position);

		Task<IEnumerable<IPosition>> GetByClentAsync(string clientId, string[] assets);

		Task<IEnumerable<IPosition>> GetByAssetAsync(string asset);

		Task<IEnumerable<IPosition>> GetAllAsync();

		bool Any();
	}
}
