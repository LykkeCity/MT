using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingTransactionRepository
    {
        Task AddAsync(ITransaction transaction);

        Task<IEnumerable<ITransaction>> GetTransactionsAsync(DateTime? from = null, DateTime? to = null);

        Task<IEnumerable<ITransaction>> GetTransactionsByMarketMakerAsync(string marketMakerId, string[] assets, DateTime? from = null, DateTime? to = null);

        bool Any();
    }
}
