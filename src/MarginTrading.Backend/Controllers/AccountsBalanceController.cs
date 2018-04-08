using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.AccountBalance;
using MarginTrading.Backend.Contracts.Common;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Services;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class AccountsBalanceController : Controller, IAccountsBalanceApi
    {
        private readonly IMarginTradingOperationsLogService _operationsLogService;
        private readonly ILog _log;
        private readonly MarginSettings _marginSettings;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly AccountManager _accountManager;

        public AccountsBalanceController(
            MarginSettings marginSettings,
            IMarginTradingOperationsLogService operationsLogService,
            ILog log,
            IAccountsCacheService accountsCacheService,
            AccountManager accountManager)
        {
            _marginSettings = marginSettings;
            _operationsLogService = operationsLogService;
            _log = log;
            _accountsCacheService = accountsCacheService;
            _accountManager = accountManager;
        }
        
        
        [Route("deposit")]
        [HttpPost]
        [ProducesResponseType(typeof(BackendResponse<AccountDepositWithdrawResponse>), 200)]
        public async Task<BackendResponse<AccountDepositWithdrawResponse>> AccountDeposit([FromBody]AccountDepositWithdrawRequest request)
        {
            var account = _accountsCacheService.Get(request.ClientId, request.AccountId);

            var changeTransferLimit = _marginSettings.IsLive &&
                                      request.PaymentType == PaymentType.Transfer &&
                                      !IsCrypto(account.BaseAssetId);

            try
            {
                var transactionId = await _accountManager.UpdateBalanceAsync(account, Math.Abs(request.Amount),
                    AccountHistoryType.Deposit, "Account deposit", request.TransactionId, changeTransferLimit);
                
                _operationsLogService.AddLog($"account deposit {request.PaymentType}", request.ClientId, request.AccountId, request.ToJson(), true.ToJson());

                return BackendResponse<AccountDepositWithdrawResponse>.Ok(
                    new AccountDepositWithdrawResponse {TransactionId = transactionId});
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(AccountsBalanceController), "AccountDeposit", request?.ToJson(), e);
                return BackendResponse<AccountDepositWithdrawResponse>.Error(e.Message);
            }
        }

        [Route("withdraw")]
        [HttpPost]
        [ProducesResponseType(typeof(BackendResponse<AccountDepositWithdrawResponse>), 200)]
        public async Task<BackendResponse<AccountDepositWithdrawResponse>> AccountWithdraw([FromBody]AccountDepositWithdrawRequest request)
        {
            var account = _accountsCacheService.Get(request.ClientId, request.AccountId);
            var freeMargin = account.GetFreeMargin();

            if (freeMargin < Math.Abs(request.Amount))
                return BackendResponse<AccountDepositWithdrawResponse>.Error(
                    "Requested withdrawal amount is less than free margin");

            var changeTransferLimit = _marginSettings.IsLive &&
                                      request.PaymentType == PaymentType.Transfer &&
                                      !IsCrypto(account.BaseAssetId);

            try
            {
                var transactionId = await _accountManager.UpdateBalanceAsync(account, -Math.Abs(request.Amount),
                    AccountHistoryType.Withdraw, "Account withdraw", null, changeTransferLimit);
                
                _operationsLogService.AddLog($"account withdraw {request.PaymentType}", request.ClientId, request.AccountId, request.ToJson(), true.ToJson());
                
                return BackendResponse<AccountDepositWithdrawResponse>.Ok(
                    new AccountDepositWithdrawResponse {TransactionId = transactionId});

            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(AccountsBalanceController), "AccountWithdraw", request?.ToJson(), e);
                return BackendResponse<AccountDepositWithdrawResponse>.Error(e.Message);
            }
        }

        /// <summary>
        /// Manually charge client's account. Amount is absolute, i.e. negative value goes for charging.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Route("chargeManually")]
        [HttpPost]
        [ProducesResponseType(typeof(BackendResponse<AccountChargeManuallyResponse>), 200)]
        public async Task<BackendResponse<AccountChargeManuallyResponse>> ChargeManually([FromBody]AccountChargeManuallyRequest request)
        {
            if (string.IsNullOrEmpty(request?.Reason?.Trim()))
            {
                return BackendResponse<AccountChargeManuallyResponse>.Error("Reason must be set.");
            }
            
            var account = _accountsCacheService.Get(request.ClientId, request.AccountId);
            
            try
            {
                var transactionId = await _accountManager.UpdateBalanceAsync(account, request.Amount,
                    AccountHistoryType.Manual, request.Reason, auditLog: request.ToJson());

                _operationsLogService.AddLog("account charge manually", request.ClientId, request.AccountId,
                    request.ToJson(), true.ToJson());

                return BackendResponse<AccountChargeManuallyResponse>.Ok(
                    new AccountChargeManuallyResponse {TransactionId = transactionId});

            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(AccountsBalanceController), "ChargeManually", request?.ToJson(), e);
                return BackendResponse<AccountChargeManuallyResponse>.Error(e.Message);
            }
        }

        [Route("reset")]
        [HttpPost]
        [ProducesResponseType(typeof(BackendResponse<AccountResetResponse>), 200)]
        public async Task<BackendResponse<AccountResetResponse>> AccountResetDemo([FromBody]AccounResetRequest request)
        {
            if (_marginSettings.IsLive)
                return BackendResponse<AccountResetResponse>.Error("Account reset is available only for DEMO accounts");

            var transactionId = await _accountManager.ResetAccountAsync(request.ClientId, request.AccountId);

            _operationsLogService.AddLog("account reset", request.ClientId, request.AccountId, request.ToJson(), true.ToJson());

            return BackendResponse<AccountResetResponse>.Ok(
                new AccountResetResponse {TransactionId = transactionId});
        }

        #region Obsolete
        
        [Route("~/api/backoffice/marginTradingAccounts/deposit")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        [Obsolete]
        public async Task<bool> AccountDepositOld([FromBody] AccountDepositWithdrawRequest request)
        {
            return (await AccountDeposit(request)).IsOk;
        }
        
        [Route("~/api/backoffice/marginTradingAccounts/withdraw")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        [Obsolete]
        public async Task<bool> AccountWithdrawOld([FromBody] AccountDepositWithdrawRequest request)
        {
            return (await AccountWithdraw(request)).IsOk;
        }
        
        [Route("~/api/backoffice/marginTradingAccounts/reset")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        [Obsolete]
        public async Task<bool> AccountResetDemoOld([FromBody] AccounResetRequest request)
        {
            return (await AccountResetDemo(request)).IsOk;
        }
        
        #endregion
        
        private bool IsCrypto(string baseAssetId)
        {
            return baseAssetId == LykkeConstants.BitcoinAssetId
                   || baseAssetId == LykkeConstants.LykkeAssetId
                   || baseAssetId == LykkeConstants.EthAssetId
                   || baseAssetId == LykkeConstants.SolarAssetId;
        }
    }
}