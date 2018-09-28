using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Helpers;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Repositories;
using MarginTrading.Common.Services;

namespace MarginTrading.Backend.Services
{
    public class AccountsCacheService : IAccountsCacheService
    {
        private Dictionary<string, MarginTradingAccount> _accounts = new Dictionary<string, MarginTradingAccount>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        private readonly IDateService _dateService;
        private readonly ILog _log;

        public AccountsCacheService(
            IDateService dateService,
            ILog log)
        {
            _dateService = dateService;
            _log = log;
        }
        
        public IReadOnlyList<MarginTradingAccount> GetAll()
        {
            return _accounts.Values.ToArray();
        }

        public PaginatedResponse<MarginTradingAccount> GetAllByPages(int? skip = null, int? take = null)
        {
            var accounts = _accounts.Values.OrderBy(x => x.Id).ToList();//todo think again about ordering
            var data = (!take.HasValue ? accounts : accounts.Skip(skip.Value))
                .Take(PaginationHelper.GetTake(take)).ToList();
            return new PaginatedResponse<MarginTradingAccount>(
                contents: data,
                start: skip ?? 0,
                size: data.Count,
                totalSize: accounts.Count
            );
        }

        public MarginTradingAccount Get(string accountId)
        {
            return TryGetAccount(accountId) ??
                throw new AccountNotFoundException(accountId, string.Format(MtMessages.AccountByIdNotFound, accountId));
        }

        public MarginTradingAccount TryGet(string accountId)
        {
            return TryGetAccount(accountId);
        }

        private MarginTradingAccount TryGetAccount(string accountId)
        {
            _lockSlim.EnterReadLock();
            try
            {
                _accounts.TryGetValue(accountId, out var result);

                return result;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        internal void InitAccountsCache(Dictionary<string, MarginTradingAccount> accounts)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _accounts = accounts;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public async Task<bool> UpdateAccountChanges(string accountId, string updatedTradingConditionId,
            decimal updatedWithdrawTransferLimit, bool isDisabled, bool isWithdrawalDisabled, DateTime eventTime)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var account = _accounts[accountId];

                if (account.LastUpdateTime > eventTime)
                {
                    await _log.WriteInfoAsync(nameof(AccountsCacheService), nameof(UpdateAccountChanges), 
                        $"Account with id {account.Id} is in newer state then the event");
                    return false;
                } 
                
                account.TradingConditionId = updatedTradingConditionId;
                account.WithdrawTransferLimit = updatedWithdrawTransferLimit;
                account.IsDisabled = isDisabled;
                account.IsWithdrawalDisabled = isWithdrawalDisabled;
                account.LastUpdateTime = eventTime;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
            return true;
        }

        public async Task<bool> UpdateAccountBalance(string accountId, decimal accountBalance, DateTime eventTime)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var account = _accounts[accountId];

                if (account.LastBalanceChangeTime > eventTime)
                {
                    await _log.WriteInfoAsync(nameof(AccountsCacheService), nameof(UpdateAccountChanges), 
                        $"Account with id {account.Id} has balance in newer state then the event");
                    return false;
                } 
                
                account.Balance = accountBalance;
                account.LastBalanceChangeTime = eventTime;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
            return true;
        }

        public void TryAddNew(MarginTradingAccount account)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                account.LastUpdateTime = _dateService.Now();
                _accounts.TryAdd(account.Id, account);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
    }
}