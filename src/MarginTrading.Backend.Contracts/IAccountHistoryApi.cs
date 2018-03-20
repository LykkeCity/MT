using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Contract.BackendContracts;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    public interface IAccountHistoryApi
    {
        [Get("/api/accountHistory/byTypes")]
        Task<AccountHistoryBackendResponse> ByTypes([Query] AccountHistoryBackendRequest request);

        [Get("/api/accountHistory/byAccounts")]
        Task<Dictionary<string, AccountHistoryBackendContract[]>> ByAccounts(
            [Query] string accountId = null, [Query] DateTime? from = null, [Query] DateTime? to = null);

        [Get("/api/accountHistory/timeline")]
        Task<AccountNewHistoryBackendResponse> Timeline([Query] AccountHistoryBackendRequest request);
    }
}