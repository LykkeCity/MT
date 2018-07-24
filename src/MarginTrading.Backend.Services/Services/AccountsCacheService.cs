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

        private readonly IAccountMarginFreezingRepository _accountMarginFreezingRepository;

        public AccountsCacheService(IAccountMarginFreezingRepository accountMarginFreezingRepository)
        {
            _accountMarginFreezingRepository = accountMarginFreezingRepository;
        }
        
        public IReadOnlyList<MarginTradingAccount> GetAll()
        {
            return _accounts.Values.ToArray();
        }

        public PaginatedResponse<MarginTradingAccount> GetAllByPages(int? skip = null, int? take = null)
        {
            var accounts = _accounts.Values.OrderBy(x => x.Id).ToList();//todo think again about ordering
            return new PaginatedResponse<MarginTradingAccount>(
                contents: !take.HasValue ? accounts : accounts.Skip(skip.Value).Take(PaginationHelper.GetTake(take)).ToList(),
                start: skip ?? 0,
                size: take ?? accounts.Count,
                totalSize: accounts.Count
            );
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

                var marginFreezings = _accountMarginFreezingRepository.GetAllAsync().GetAwaiter().GetResult()
                    .GroupBy(x => x.AccountId)
                    .ToDictionary(x => x.Key, x => x.ToDictionary(z => z.OperationId, z => z.Amount));
                foreach (var account in accounts.Select(x => x.Value))
                {
                    account.AccountFpl.WithdrawalFrozenMarginData = marginFreezings.TryGetValue(account.Id, out var freezings)
                        ? freezings
                        : new Dictionary<string, decimal>();
                    account.AccountFpl.WithdrawalFrozenMargin = account.AccountFpl.WithdrawalFrozenMarginData.Sum(x => x.Value);
                }
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public async Task FreezeWithdrawalMargin(string operationId, string clientId, string accountId, decimal amount)
        {
            await _accountMarginFreezingRepository.TryInsertAsync(new AccountMarginFreezing(operationId,
                clientId, accountId, amount));
        }

        public async Task UnfreezeWithdrawalMargin(string operationId)
        {
            await _accountMarginFreezingRepository.DeleteAsync(operationId);
        }

        public void UpdateAccountChanges(string accountId, string updatedTradingConditionId, decimal updatedBalance,
            decimal updatedWithdrawTransferLimit)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var account = _accounts[accountId];
                account.TradingConditionId = updatedTradingConditionId;
                account.Balance = updatedBalance;
                account.WithdrawTransferLimit = updatedWithdrawTransferLimit;
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