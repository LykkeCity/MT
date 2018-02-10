using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace MarginTrading.Backend.Core
{
    public interface IMarginTradingAccount
    {
        string Id { get; }
        string TradingConditionId { get; }
        string ClientId { get; }
        string BaseAssetId { get; }
        decimal Balance { get; }
        decimal WithdrawTransferLimit { get; }
        [NotNull]
        AccountFpl FplData { get; }
    }

    public class MarginTradingAccount : IMarginTradingAccount, IComparable<MarginTradingAccount>
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }

        public AccountFpl FplData { get; }

        public MarginTradingAccount()
        {
            FplData = new AccountFpl();
        }
        
        public static MarginTradingAccount Create(IMarginTradingAccount src)
        {
            return new MarginTradingAccount
            {
                Id = src.Id,
                TradingConditionId = src.TradingConditionId,
                ClientId = src.ClientId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit
            };
        }

        public int CompareTo(MarginTradingAccount other)
        {
            var result = string.Compare(Id, other.Id, StringComparison.Ordinal);
            
            if(0 != result)
                return result;

            return string.Compare(ClientId, other.ClientId, StringComparison.Ordinal);
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

        Task<MarginTradingAccount> UpdateBalanceAsync(string clientId, string accountId, decimal amount,
            bool changeLimit);

        Task<IMarginTradingAccount> UpdateTradingConditionIdAsync(string clientId, string accountId,
            string tradingConditionId);

        Task AddAsync(MarginTradingAccount account);
        Task DeleteAsync(string clientId, string accountId);
    }

    public static class MarginTradingAccountExtensions
    {
        private static AccountFpl GetAccountFpl(this IMarginTradingAccount account)
        {
            if (account.FplData.ActualHash != account.FplData.CalculatedHash)
            {
                MtServiceLocator.AccountUpdateService.UpdateAccount(account);
            }

            return account.FplData;
        }

        public static AccountLevel GetAccountLevel(this IMarginTradingAccount account)
        {
            var marginUsageLevel = account.GetMarginUsageLevel();

            if (marginUsageLevel <= account.GetStopOutLevel())
                return AccountLevel.StopOUt;

            if (marginUsageLevel <= account.GetMarginCallLevel())
                return AccountLevel.MarginCall;

            return AccountLevel.None;
        }

        public static decimal GetMarginUsageLevel(this IMarginTradingAccount account)
        {
            var totalCapital = account.GetTotalCapital();
            
            var usedMargin = account.GetUsedMargin();

            //Anton Belkin said 100 is ok )
            if (usedMargin <= 0)
                return 100;

            return totalCapital / usedMargin;
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

        public static decimal GetMarginCallLevel(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().MarginCallLevel;
        }

        public static decimal GetStopOutLevel(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().StopoutLevel;
        }

        public static int GetOpenPositionsCount(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().OpenPositionsCount;
        }

        public static void CacheNeedsToBeUpdated(this IMarginTradingAccount account)
        {
            account.FplData.ActualHash++;
        }
    }
}

