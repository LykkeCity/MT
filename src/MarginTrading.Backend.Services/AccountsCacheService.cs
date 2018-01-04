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
        private Dictionary<string, MarginTradingAccount[]> _accounts = new Dictionary<string, MarginTradingAccount[]>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();

        public void UpdateAccountsCache(string clientId, IEnumerable<MarginTradingAccount> newValues)
        {
            var newInstances = newValues.Select(MarginTradingAccount.Create);
            UpdateClientAccounts(clientId, newInstances);
        }

        public IEnumerable<MarginTradingAccount> GetAll(string clientId)
        {
            return GetClientAccounts(clientId);
        }

        public IReadOnlyList<MarginTradingAccount> GetAll()
        {
            return GetClientAccounts(null);
        }

        public MarginTradingAccount Get(string clientId, string accountId)
        {
            var result = GetClientAccount(clientId, accountId);
            if (null == result)
                throw new AccountNotFoundException(accountId, string.Format(MtMessages.AccountByIdNotFound, accountId));

            return result;
        }
        
        public MarginTradingAccount TryGet(string clientId, string accountId)
        {
            return GetClientAccount(clientId, accountId);
        }

        public void UpdateBalance(MarginTradingAccount account)
        {
            UpdateAccount(account.ClientId, account.Id, x =>
            {
                x.Balance = account.Balance;
                x.WithdrawTransferLimit = account.WithdrawTransferLimit;
            });
        }

        public IMarginTradingAccount SetTradingCondition(string clientId, string accountId, string tradingConditionId)
        {
            UpdateAccount(clientId, accountId, account => account.TradingConditionId = tradingConditionId);

            return GetClientAccount(clientId, accountId, true);
        }

        public IEnumerable<string> GetClientIdsByTradingConditionId(string tradingConditionId, string accountId = null)
        {
            _lockSlim.EnterReadLock();
            try
            {
                foreach (var clientId in _accounts.Keys)
                    if (_accounts[clientId].Any(item => item.TradingConditionId == tradingConditionId &&
                        (string.IsNullOrEmpty(accountId) || item.Id == accountId)))
                        yield return clientId;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        private MarginTradingAccount[] GetClientAccounts(string clientId)
        {
            _lockSlim.EnterReadLock();
            try
            {
                if (clientId != null)
                {
                    if (_accounts.ContainsKey(clientId))
                        return _accounts[clientId];

                    return Array.Empty<MarginTradingAccount>();
                }
                else
                {
                    return _accounts.SelectMany(a => a.Value).ToArray();
                }
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        private void UpdateClientAccounts(string clientId, IEnumerable<MarginTradingAccount> newValue)
        {
            var accounts = newValue.ToArray();

            _lockSlim.EnterWriteLock();
            try
            {
                if (!_accounts.ContainsKey(clientId))
                    _accounts.Add(clientId, accounts);
                else
                    _accounts[clientId] = accounts;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        private MarginTradingAccount GetClientAccount(string clientId, string accountId, bool throwIfNotExists = false)
        {
            var accounts = GetClientAccounts(clientId);

            if (accounts.Length == 0)
            {
                if (throwIfNotExists)
                    throw new Exception(string.Format(MtMessages.ClientIdNotFoundInCache, clientId));

                return null;
            }

            _lockSlim.EnterReadLock();
            try
            {
                var result = accounts.FirstOrDefault(x => x.Id == accountId);

                if (null == result && throwIfNotExists)
                    throw new Exception(string.Format(MtMessages.ClientAccountNotFoundInCache, clientId,
                        accountId));

                return result;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        private void UpdateAccount(string clientId, string accountId, Action<MarginTradingAccount> updateAction)
        {
            var account = GetClientAccount(clientId, accountId, true);
            updateAction(account);
        }

        internal void InitAccountsCache(Dictionary<string, MarginTradingAccount[]> accounts)
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
