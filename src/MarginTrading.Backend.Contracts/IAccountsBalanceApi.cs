using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AccountBalance;
using MarginTrading.Backend.Contracts.Common;
using Refit;

namespace MarginTrading.Backend.Contracts
{
    /// <summary>
    /// Account deposit, withdraw and other operations with balace
    /// </summary>
    [PublicAPI]
    public interface IAccountsBalanceApi
    {
        /// <summary>
        /// Add funds to account
        /// </summary>
        [Get("/api/AccountsBalance/deposit")]
        Task<BackendResponse<AccountDepositWithdrawResponse>> AccountDeposit(AccountDepositWithdrawRequest request);
        
        /// <summary>
        /// Remove funds from account
        /// </summary>
        [Get("/api/AccountsBalance/withdraw")]
        Task<BackendResponse<AccountDepositWithdrawResponse>> AccountWithdraw(AccountDepositWithdrawRequest request);
        
        /// <summary>
        /// Gets schedule settings
        /// </summary>
        /// <remarks>
        /// Only for DEMO account
        /// </remarks>
        [Get("/api/AccountsBalance/reset")]
        Task<BackendResponse<AccountResetResponse>> AccountResetDemo(AccounResetRequest request);
    }
}