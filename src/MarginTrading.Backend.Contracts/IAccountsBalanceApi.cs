using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.Backend.Contracts.AccountBalance;
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
        Task<bool> AccountDeposit(AccountDepositWithdrawRequest request);
        
        /// <summary>
        /// Remove funds from account
        /// </summary>
        [Get("/api/AccountsBalance/withdraw")]
        Task<bool> AccountWithdraw(AccountDepositWithdrawRequest request);
        
        /// <summary>
        /// Gets schedule settings
        /// </summary>
        /// <remarks>
        /// Only for DEMO account
        /// </remarks>
        [Get("/api/AccountsBalance/reset")]
        Task<bool> AccountResetDemo(AccounResetRequest request);
    }
}