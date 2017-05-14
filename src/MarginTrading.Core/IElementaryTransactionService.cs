using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
	public interface IElementaryTransactionService
	{
		Task CreateElementaryTransactionsAsync(ITransaction transaction, Func<IElementaryTransaction, Task> destination);

		Task CreateElementaryTransactionsFromTransactionReport(Func<Task<IEnumerable<ITransaction>>> source, Func<IElementaryTransaction, Task> destination);
	}
}