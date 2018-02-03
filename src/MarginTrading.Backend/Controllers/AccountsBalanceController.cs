using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.AccountBalance;
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
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<bool> AccountDeposit([FromBody]AccountDepositWithdrawRequest request)
        {
            var account = _accountsCacheService.Get(request.ClientId, request.AccountId);

            var changeTransferLimit = _marginSettings.IsLive &&
                                      request.PaymentType == PaymentType.Transfer &&
                                      !IsCrypto(account.BaseAssetId);

            try
            {
                await _accountManager.UpdateBalanceAsync(account, Math.Abs(request.Amount),
                    AccountHistoryType.Deposit, "Account deposit", null/*TODO: transaction ID*/, changeTransferLimit);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BackOfficeController), "AccountDeposit", request?.ToJson(), e);
                return false;
            }

            _operationsLogService.AddLog($"account deposit {request.PaymentType}", request.ClientId, request.AccountId, request.ToJson(), true.ToJson());

            return true;
        }

        [Route("withdraw")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<bool> AccountWithdraw([FromBody]AccountDepositWithdrawRequest request)
        {
            var account = _accountsCacheService.Get(request.ClientId, request.AccountId);
            var freeMargin = account.GetFreeMargin();

            if (freeMargin < Math.Abs(request.Amount))
                return false;

            var changeTransferLimit = _marginSettings.IsLive &&
                                      request.PaymentType == PaymentType.Transfer &&
                                      !IsCrypto(account.BaseAssetId);

            try
            {
                await _accountManager.UpdateBalanceAsync(account, -Math.Abs(request.Amount),
                    AccountHistoryType.Withdraw, "Account withdraw", null, changeTransferLimit);
            }
            catch (Exception e)
            {
                await _log.WriteErrorAsync(nameof(BackOfficeController), "AccountWithdraw", request?.ToJson(), e);
                return false;
            }

            _operationsLogService.AddLog($"account withdraw {request.PaymentType}", request.ClientId, request.AccountId, request.ToJson(), true.ToJson());

            return true;
        }

        [Route("reset")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<bool> AccountResetDemo([FromBody]AccounResetRequest request)
        {
            if (_marginSettings.IsLive)
                return false;

            await _accountManager.ResetAccountAsync(request.ClientId, request.AccountId);

            _operationsLogService.AddLog("account reset", request.ClientId, request.AccountId, request.ToJson(), true.ToJson());

            return true;
        }

        #region Obsolete
        
        [Route("~/api/backoffice/marginTradingAccounts/deposit")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        [Obsolete]
        public Task<bool> AccountDepositOld([FromBody] AccountDepositWithdrawRequest request)
        {
            return AccountDeposit(request);
        }
        
        [Route("~/api/backoffice/marginTradingAccounts/withdraw")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        [Obsolete]
        public Task<bool> AccountWithdrawOld([FromBody] AccountDepositWithdrawRequest request)
        {
            return AccountWithdraw(request);
        }
        
        [Route("~/api/backoffice/marginTradingAccounts/reset")]
        [HttpPost]
        [ProducesResponseType(typeof(bool), 200)]
        [Obsolete]
        public Task<bool> AccountResetDemoOld([FromBody] AccounResetRequest request)
        {
            return AccountResetDemo(request);
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