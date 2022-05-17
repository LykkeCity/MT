// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Exceptions;
using MarginTrading.Backend.Core.Helpers;
using MarginTrading.Backend.Core.Messages;
using MarginTrading.Backend.Services.Services;
using MarginTrading.Common.Services;
using MoreLinq;
using StackExchange.Redis;

namespace MarginTrading.Backend.Services
{
    public class AccountsCacheService : IAccountsCacheService
    {
        private readonly struct AccountLiquidationInfo
        {
            public AccountLiquidationInfo(string operationId, string accountId)
            {
                OperationId = operationId;
                AccountId = accountId;
            }

            public string OperationId { get; }
            
            public string AccountId { get; }
        }
        
        private Dictionary<string, MarginTradingAccount> _accounts = new Dictionary<string, MarginTradingAccount>();
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        private readonly IConnectionMultiplexer _redis;
        private readonly IDateService _dateService;
        private readonly ILog _log;
        
        private const string RedisKeyFmt = "core:account:{0}:current-liquidation";

        public AccountsCacheService(
            IDateService dateService,
            ILog log,
            IConnectionMultiplexer redis)
        {
            _dateService = dateService;
            _log = log;
            _redis = redis;
        }
        
        public IReadOnlyList<MarginTradingAccount> GetAll()
        {
            return _accounts.Values.ToArray();
        }

        public IAsyncEnumerable<MarginTradingAccount> GetAllInLiquidation()
        {
            var accountIds = _accounts.Select(a => a.Value.Id);

            var liquidationInfoList = GetLiquidationInfo(accountIds.ToArray());

            return liquidationInfoList
                .Select(i => _accounts.SingleOrDefault(a => a.Value.Id == i.AccountId).Value)
                .Where(a => a != null);
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

        public async Task<string> GetLiquidationOperationId(string accountId)
        {
            var info = await GetLiquidationInfo(new[] { accountId }).ToListAsync();

            return info.First().OperationId;
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
                return _accounts.TryGetValue(accountId, out var result) ? result : null;
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

        public void ResetTodayProps()
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _accounts.Values.ForEach(x =>
                {
                    x.TodayStartBalance = x.Balance;
                    x.TodayRealizedPnL = 0;
                    x.TodayUnrealizedPnL = 0;
                    x.TodayDepositAmount = 0;
                    x.TodayWithdrawAmount = 0;
                    x.TodayCommissionAmount = 0;
                    x.TodayOtherAmount = 0;
                });
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        }

        public async Task<(bool, string)> TryStartLiquidation(string accountId, string operationId)
        {
            if (!_accounts.TryGetValue(accountId, out _))
                return (false, string.Empty);

            var liquidationInfoAdded = await AddLiquidationInfo(accountId, operationId);

            if (liquidationInfoAdded)
                return (true, operationId);

            var existingLiquidationOperationId = await GetLiquidationOperationId(accountId);

            if (existingLiquidationOperationId == null)
            {
                // potentially, it is possible in a highly concurrent environments
                return (false, string.Empty);
            }

            return (false, existingLiquidationOperationId);
        }

        public async Task<bool> TryFinishLiquidation(string accountId, string reason, 
            string liquidationOperationId = null)
        {
            if (!_accounts.TryGetValue(accountId, out var account))
                return false;

            var existingLiquidationOperationId = await GetLiquidationOperationId(accountId);
            
            if (existingLiquidationOperationId == null)
                return false;

            if (string.IsNullOrEmpty(liquidationOperationId) ||
                liquidationOperationId == existingLiquidationOperationId)
            {
                await RemoveLiquidationInfo(accountId);

                _log.WriteInfo(nameof(TryFinishLiquidation), account,
                    $"Liquidation state was removed for account {accountId}. Reason: {reason}");
                return true;
            }

            _log.WriteInfo(nameof(TryFinishLiquidation), account,
                $"Liquidation state was not removed for account {accountId} " +
                $"by liquidationOperationId {liquidationOperationId} " +
                $"Current LiquidationOperationId: {existingLiquidationOperationId}.");
            
            return false;
        }

        public async Task<bool> IsInLiquidation(string accountId)
        {
            var liquidationOperationId = await GetLiquidationOperationId(accountId);

            return liquidationOperationId != null;
        }

        public async Task<bool> UpdateAccountChanges(string accountId, string updatedTradingConditionId,
            decimal updatedWithdrawTransferLimit, bool isDisabled, bool isWithdrawalDisabled, DateTime eventTime, string additionalInfo)
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
                account.AdditionalInfo = additionalInfo;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
            return true;
        }

        public async Task<bool> HandleBalanceChange(string accountId,
            decimal accountBalance, decimal changeAmount, AccountBalanceChangeReasonType reasonType, DateTime eventTime)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                var account = _accounts[accountId];

                switch (reasonType)
                {
                    case AccountBalanceChangeReasonType.RealizedPnL:
                        account.TodayRealizedPnL += changeAmount;
                        break;
                    case AccountBalanceChangeReasonType.UnrealizedDailyPnL:
                        account.TodayUnrealizedPnL += changeAmount;
                        account.TodayOtherAmount += changeAmount; // TODO: why?
                        break;
                    case AccountBalanceChangeReasonType.Deposit:
                        account.TodayDepositAmount += changeAmount;
                        break;
                    case AccountBalanceChangeReasonType.Withdraw:
                        account.TodayWithdrawAmount += changeAmount;
                        break;
                    case AccountBalanceChangeReasonType.Commission:
                        account.TodayCommissionAmount += changeAmount;
                        break;
                    default:
                        account.TodayOtherAmount += changeAmount;
                        break;
                }

                if (account.LastBalanceChangeTime > eventTime)
                {
                    await _log.WriteInfoAsync(nameof(AccountsCacheService), nameof(HandleBalanceChange), 
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

        public async Task Remove(string accountId)
        {
            bool removedFromCache;
            
            _lockSlim.EnterWriteLock();
            try
            {
                removedFromCache = _accounts.Remove(accountId);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            if (removedFromCache)
            {
                await RemoveLiquidationInfo(accountId);
            }
        }

        public async Task<string> Reset(string accountId, DateTime eventTime)
        {
            string warnings;
            
            _lockSlim.EnterWriteLock();
            try
            {
                if (!_accounts.TryGetValue(accountId, out var account))
                {
                    throw new Exception($"Account {accountId} does not exist.");
                }

                warnings = account.Reset(eventTime);
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }

            var liquidationInfoRemoved = await RemoveLiquidationInfo(accountId);
            if (liquidationInfoRemoved)
            {
                return string.Join(", ", new[] { warnings, $"Liquidation is in progress"});
            }

            return warnings;
        }

        private static string GetRedisKey(string accountId) => string.Format(RedisKeyFmt, accountId);
        
        private async IAsyncEnumerable<AccountLiquidationInfo> GetLiquidationInfo(string[] accounts)
        {
            var keys = accounts.Select(GetRedisKey);
            
            var results = await _redis
                .GetDatabase()
                .StringGetAsync(keys.Cast<RedisKey>().ToArray());
            
            foreach (var redisValue in results)
            {
                if (redisValue.HasValue)
                    yield return ProtoBufSerializer.Deserialize<AccountLiquidationInfo>(redisValue);
            }
        }

        private async Task<bool> AddLiquidationInfo(string accountId, string operationId)
        {
            var info = new AccountLiquidationInfo(operationId, accountId);

            var serialized = ProtoBufSerializer.Serialize(info);

            var added = await _redis
                .GetDatabase()
                .StringSetAsync(GetRedisKey(accountId), serialized, when: When.NotExists);

            return added;
        }

        private Task<bool> RemoveLiquidationInfo(string accountId) =>
            _redis
                .GetDatabase()
                .KeyDeleteAsync(GetRedisKey(accountId));
    }
}