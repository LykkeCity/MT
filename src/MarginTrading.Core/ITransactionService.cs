using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface ITransactionService
	{
		Task CreateTransactionsForCancelledOrderAsync(IOrder position, Func<ITransaction, Task> destination, bool realtime = true);

		Task CreateTransactionsForClosedOrderAsync(IOrder position, Func<ITransaction, Task> destination, bool realtime = true);

		Task CreateTransactionsForOpenOrderAsync(IOrder position, Func<ITransaction, Task> destination, bool realtime = true);

		Task CreateTransactionsForOrderHistory(Func<Task<IEnumerable<IOrderHistory>>> source, Func<ITransaction, Task> destination);
	}
}
