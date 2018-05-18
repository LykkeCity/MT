using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Messages;

namespace MarginTrading.Backend.Services
{
    public class AccountsCacheService : IAccountsCacheService
    {
        private Dictionary<string, MarginTradingAccount> _accounts = new Dictionary<string, MarginTradingAccount>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public IReadOnlyList<MarginTradingAccount> GetAll()
        {
            return _accounts.Values.ToArray();
        }

        public MarginTradingAccount Get(string accountId)
        {
            return GetAccount(accountId) ??
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
            return GetAccount(accountId);
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

        private MarginTradingAccount GetAccount(string accountId, bool throwIfNotExists = false)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (!_accounts.TryGetValue(accountId, out var result) && throwIfNotExists)
                    throw new Exception(string.Format(MtMessages.AccountNotFoundInCache, accountId));

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
    }
}