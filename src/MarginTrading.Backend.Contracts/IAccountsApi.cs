using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Account;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAccountsApi
    {
        /// <summary>
        ///     Returns all accounts stats
        /// </summary>
        [Get("/api/accounts/stats")]
        Task<List<AccountStatContract>> GetAllAccountStats();
        
        /// <summary>
        ///     Returns stats of selected account
        /// </summary>
        [Get("/api/accounts/stats/{accountId}")]
        Task<AccountStatContract> GetAccountStats(string accountId);
    }
}