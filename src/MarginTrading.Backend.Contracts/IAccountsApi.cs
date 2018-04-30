using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Account;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    public interface IAccountsApi
    {
        [Get("/api/accounts/stats")]
        Task<IEnumerable<DataReaderAccountStatsBackendContract>> GetAllAccountStats();
    }
}