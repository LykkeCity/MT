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
        ///     Returns all account stats
        /// </summary>
        Task<IEnumerable<DataReaderAccountStatsBackendContract>> GetAllAccountStats();
    }
}