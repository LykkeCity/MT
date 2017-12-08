using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
{
    public interface IMarginTradingAccount
    {
        string Id { get; }
        string TradingConditionId { get; }
        string ClientId { get; }
        string BaseAssetId { get; }
        double Balance { get; }
        double WithdrawTransferLimit { get; }
    }

    public class MarginTradingAccount : IMarginTradingAccount, IComparable<MarginTradingAccount>
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public double Balance { get; set; }
        public double WithdrawTransferLimit { get; set; }
       
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
        Task<IMarginTradingAccount> GetAsync(string clientId, string accountId);
        Task<IMarginTradingAccount> GetAsync(string accountId);
        Task<MarginTradingAccount> UpdateBalanceAsync(string clientId, string accountId, double amount, bool changeLimit);
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

        public static double GetMarginUsageLevel(this MarginTradingAccount account)
        {
            var totalCapital = account.GetTotalCapital();
            return totalCapital == 0 ? 0 : Math.Abs(account.GetUsedMargin() / totalCapital);
        }

        public static double GetTotalCapital(this IMarginTradingAccount account)
        {
            return account.Balance + account.GetPnl();
        }

        public static double GetPnl(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().PnL;
        }

        public static double GetUsedMargin(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().UsedMargin;
        }

        public static double GetFreeMargin(this IMarginTradingAccount account)
        {
            return account.GetTotalCapital() - account.GetUsedMargin();
        }

        public static double GetMarginInit(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().MarginInit;
        }

        public static double GetMarginAvailable(this IMarginTradingAccount account)
        {
            return account.GetTotalCapital() - account.GetMarginInit();
        }

        public static double GetMarginCall(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().MarginCall;
        }

        public static double GetStopOut(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().Stopout;
        }

        public static double GetOpenPositionsCount(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().OpenPositionsCount;
        }

        public static double GetMarginUsageLevel(this IMarginTradingAccount account)
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

