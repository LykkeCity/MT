using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Helpers;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.Backend.Services
{
    public class AccountsCacheService : IAccountsCacheService
    {
        private Dictionary<string, MarginTradingAccount> _accounts = new Dictionary<string, MarginTradingAccount>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public AccountsCacheService()
        {
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

        public void Update(MarginTradingAccount newValue)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _accounts[newValue.Id] = newValue;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public MarginTradingAccount TryGet(string accountId)
        {
            return TryGetAccount(accountId);
        }

        public IEnumerable<string> GetClientIdsByTradingConditionId(string tradingConditionId, string accountId = null)
        {
            _lockSlim.EnterReadLock();
            try
            {
                foreach (var account in _accounts.Values)
                {
                    if (account.TradingConditionId == tradingConditionId &&
                        (string.IsNullOrEmpty(accountId) || account.Id == accountId))
                        yield return account.ClientId;
                }
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
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

        public void UpdateAccountChanges(string accountId, string updatedTradingConditionId, decimal updatedBalance,
            decimal updatedWithdrawTransferLimit, bool isDisabled)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var account = _accounts[accountId];
                account.TradingConditionId = updatedTradingConditionId;
                account.Balance = updatedBalance;
                account.WithdrawTransferLimit = updatedWithdrawTransferLimit;
                account.IsDisabled = isDisabled;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public void TryAddNew(MarginTradingAccount account)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _accounts.TryAdd(account.Id, account);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }
    }
}