using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Common;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    [PublicAPI]
    public interface IAccountsApi
    {
        /// <summary>
        /// Returns all accounts stats
        /// </summary>
        [Get("/api/accounts/stats")]
        Task<List<AccountStatContract>> GetAllAccountStats();
        
        /// <summary>
        /// Returns all accounts stats, optionally paginated. Both skip and take must be set or unset.
        /// </summary>
        [Get("/api/accounts/stats/by-pages")]
        Task<PaginatedResponseContract<AccountStatContract>> GetAllAccountStatsByPages(
            [Query, CanBeNull] int? skip = null, [Query, CanBeNull] int? take = null);
        
        /// <summary>
        /// Returns stats of selected account
        /// </summary>
        [Get("/api/accounts/stats/{accountId}")]
        Task<AccountStatContract> GetAccountStats([NotNull] string accountId);

        /// <summary>
        /// Resumes liquidation of selected account
        /// </summary>
        [Post("/api/accounts/resume-liquidation/{accountId}")]
        Task ResumeLiquidation(string accountId, string comment);
    }
}