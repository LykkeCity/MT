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
        [Post("/api/AccountsBalance/deposit")]
        Task<BackendResponse<AccountDepositWithdrawResponse>> AccountDeposit(AccountDepositWithdrawRequest request);
        
        /// <summary>
        /// Remove funds from account
        /// </summary>
        [Post("/api/AccountsBalance/withdraw")]
        Task<BackendResponse<AccountDepositWithdrawResponse>> AccountWithdraw(AccountDepositWithdrawRequest request);
        
        /// <summary>
        /// Resets DEMO account balance to default
        /// </summary>
        /// <remarks>
        /// Only for DEMO account
        /// </remarks>
        [Post("/api/AccountsBalance/reset")]
        Task<BackendResponse<AccountResetResponse>> AccountResetDemo(AccounResetRequest request);

        /// <summary>
        /// Manually charge client's account. Amount is absolute, i.e. negative value goes for charging.
        /// </summary>
        [Post("/api/AccountsBalance/chargeManually")]
        Task<BackendResponse<AccountChargeManuallyResponse>> ChargeManually([Body]AccountChargeManuallyRequest request);
    }
}