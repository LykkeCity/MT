using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.Core
{
    public interface IMarginTradingAccount
    {
        string Id { get; }
        string TradingConditionId { get; }
        string ClientId { get; }
        string BaseAssetId { get; }
        decimal Balance { get; }
        decimal WithdrawTransferLimit { get; }
    }

    public class MarginTradingAccount : IMarginTradingAccount, IComparable<MarginTradingAccount>
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }

        internal AccountFpl AccountFpl;

        public static MarginTradingAccount Create(IMarginTradingAccount src)
        {
            return Create(src, null);
        }

        public static MarginTradingAccount Create(IMarginTradingAccount src, AccountFpl accountFpl)
        {
            return new MarginTradingAccount
            {
                Id = src.Id,
                TradingConditionId = src.TradingConditionId,
                ClientId = src.ClientId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                AccountFpl = accountFpl ?? new AccountFpl()
            };
        }

        public int CompareTo(MarginTradingAccount other)
        {
            var result = Id.CompareTo(other.Id);
            if(0 != result)
                return result;

            return ClientId.CompareTo(other.ClientId);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ ClientId.GetHashCode();
        }
    }

    public enum AccountLevel
    {
        None = 0,
        MarginCall = 1,
        StopOUt = 2
    }

    public interface IMarginTradingAccountsRepository
    {
        Task<IEnumerable<IMarginTradingAccount>> GetAllAsync(string clientId = null);
        [ItemCanBeNull]
        Task<IMarginTradingAccount> GetAsync(string clientId, string accountId);
        [ItemCanBeNull]
        Task<IMarginTradingAccount> GetAsync(string accountId);
        Task<MarginTradingAccount> UpdateBalanceAsync(string clientId, string accountId, decimal amount, bool changeLimit);
        Task<bool> UpdateTradingConditionIdAsync(string accountId, string tradingConditionId);
        Task AddAsync(MarginTradingAccount account);
        Task DeleteAsync(string clientId, string accountId);
    }

    public static class MarginTradingAccountExtensions
    {
        //TODO: optimize
        private static AccountFpl GetAccountFpl(this IMarginTradingAccount account)
        {
            var accountInstance = account as MarginTradingAccount;

            if (accountInstance != null)
            {
                if (accountInstance.AccountFpl == null)
                {
                    accountInstance.AccountFpl = new AccountFpl();
                    accountInstance.CacheNeedsToBeUpdated();
                }

                if (accountInstance.AccountFpl.ActualHash != accountInstance.AccountFpl.CalculatedHash)
                {
                    MtServiceLocator.AccountUpdateService.UpdateAccount(account, accountInstance.AccountFpl);
                }

                return accountInstance.AccountFpl;
            }

            MtServiceLocator.AccountUpdateService.UpdateAccount(account, account.GetAccountFpl());

            return account.GetAccountFpl();
        }

        public static AccountLevel GetAccountLevel(this MarginTradingAccount account)
        {
            var marginUsageLevel = account.GetMarginUsageLevel();

            if (marginUsageLevel >= account.GetStopOut())
                return AccountLevel.StopOUt;

            if (marginUsageLevel >= account.GetMarginCall())
                return AccountLevel.MarginCall;

            return AccountLevel.None;
        }

        public static decimal GetMarginUsageLevel(this MarginTradingAccount account)
        {
            var totalCapital = account.GetTotalCapital();

            if (totalCapital < 0)
                return decimal.MaxValue;

            if (totalCapital == 0)
            {
                if (account.Balance == 0)
                    return 0;

                return decimal.MaxValue;
            }

            return account.GetUsedMargin() / totalCapital;
        }

        public static decimal GetTotalCapital(this IMarginTradingAccount account)
        {
            return account.Balance + account.GetPnl();
        }

        public static decimal GetPnl(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().PnL;
        }

        public static decimal GetUsedMargin(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().UsedMargin;
        }

        public static decimal GetFreeMargin(this IMarginTradingAccount account)
        {
            return account.GetTotalCapital() - account.GetUsedMargin();
        }

        public static decimal GetMarginInit(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().MarginInit;
        }

        public static decimal GetMarginAvailable(this IMarginTradingAccount account)
        {
            return account.GetTotalCapital() - account.GetMarginInit();
        }

        public static decimal GetMarginCall(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().MarginCall;
        }

        public static decimal GetStopOut(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().Stopout;
        }

        public static int GetOpenPositionsCount(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().OpenPositionsCount;
        }

        public static decimal GetMarginUsageLevel(this IMarginTradingAccount account)
        {
            return account.GetTotalCapital() == 0 ? 0 : Math.Abs(account.GetUsedMargin() / account.GetTotalCapital());
        }

        public static void CacheNeedsToBeUpdated(this IMarginTradingAccount account)
        {
            var accountInstance = account as MarginTradingAccount;

            if (accountInstance != null)
            {
                if (accountInstance.AccountFpl == null)
                {
                    accountInstance.AccountFpl = new AccountFpl();
                }

                accountInstance.AccountFpl.ActualHash++;
            }
        }
    }
}

