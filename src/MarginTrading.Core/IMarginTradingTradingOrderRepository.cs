using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IMarginTradingTradingOrderRepository
	{
		Task AddAsync(ITradingOrder order);

		Task<IEnumerable<ITradingOrder>> GetOrdersAsync(DateTime? from = null, DateTime? to = null);

		bool Any();
	}
}
