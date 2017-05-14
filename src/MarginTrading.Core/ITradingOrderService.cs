using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface ITradingOrderService
	{
		Task CreateTradingOrdersForOrderHistory(Func<Task<IEnumerable<IOrderHistory>>> source, Func<ITradingOrder, Task> destination);

		Task CreateTradingOrderForClosedTakerPosition(IOrder position, Func<ITradingOrder, Task> destination, bool realtime = true);

		Task CreateTradingOrderForCancelledTakerPosition(IOrder position, Func<ITradingOrder, Task> destination, bool realtime = true);

		Task CreateTradingOrderForOpenedTakerPosition(IOrder position, Func<ITradingOrder, Task> destination, bool realtime = true);
	}
}