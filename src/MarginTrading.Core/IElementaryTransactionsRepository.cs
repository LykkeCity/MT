using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IElementaryTransactionsRepository
    {
        Task AddAsync(IElementaryTransaction transaction);

        Task<IEnumerable<IElementaryTransaction>> GetTransactionsAsync(DateTime? from = null, DateTime? to = null);

        Task<IEnumerable<IElementaryTransaction>> GetTransactionsByCounterPartyAsync(string counterParty, string[] assets, DateTime? from = null, DateTime? to = null);

        bool Any();
    }
}