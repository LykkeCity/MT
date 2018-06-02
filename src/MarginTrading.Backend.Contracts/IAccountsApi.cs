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
        ///     Returns account stats with optional filtering  
        /// </summary>
        [Get("/api/accounts/stats")]
        Task<List<AccountStatContract>> GetAllAccountStats([Query, CanBeNull] string accountId = null);
    }
}