using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.AccountHistory;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    public interface IAccountHistoryApi
    {
        [Get("/api/accountHistory/byTypes")]
        Task<AccountHistoryResponse> ByTypes([Query] AccountHistoryRequest request);

        [Get("/api/accountHistory/byAccounts")]
        Task<Dictionary<string, AccountHistoryContract[]>> ByAccounts(
            [Query] string accountId = null, [Query] DateTime? from = null, [Query] DateTime? to = null);

        [Get("/api/accountHistory/timeline")]
        Task<AccountNewHistoryResponse> Timeline([Query] AccountHistoryRequest request);
    }
}