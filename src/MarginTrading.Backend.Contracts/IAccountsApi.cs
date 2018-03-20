using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Backend.Contracts.Account;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    public interface IAccountsApi
    {
        [Get("/api/accounts/")]
        Task<IEnumerable<DataReaderAccountBackendContract>> GetAllAccounts();

        [Get("/api/accounts/stats")]
        Task<IEnumerable<DataReaderAccountStatsBackendContract>> GetAllAccountStats();

        [Get("/api/accounts/byClient/{clientId}")]
        Task<IEnumerable<DataReaderAccountBackendContract>> GetAccountsByClientId(string clientId);
        
        [Get("/api/accounts/byId/{id}")]
        Task<DataReaderAccountBackendContract> GetAccountById(string id);
    }
}