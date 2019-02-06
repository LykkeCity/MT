using System;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Settings;

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
        string LegalEntity { get; }
        [NotNull] AccountFpl AccountFpl { get; }
        bool IsDisabled { get; set; }
        bool IsDeleted { get; }
        DateTime LastUpdateTime { get; }
        DateTime LastBalanceChangeTime { get; }
        bool IsWithdrawalDisabled { get; }
        string LiquidationOperationId { get; }
    }

    public class MarginTradingAccount : IMarginTradingAccount, IComparable<MarginTradingAccount>
    {
        public string Id { get; set; }
        public string ClientId { get; set; }
        public string TradingConditionId { get; set; }
        public string BaseAssetId { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
        public string LegalEntity { get; set; }
        public bool IsDisabled { get; set; } // todo: use it everywhere
        public bool IsDeleted { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public DateTime LastBalanceChangeTime { get; set; }
        public bool IsWithdrawalDisabled { get; set; }
        public string LiquidationOperationId { get; set; }

        public AccountFpl AccountFpl { get; private set; } = new AccountFpl();

        public static MarginTradingAccount Create(IMarginTradingAccount src, AccountFpl accountFpl = null)
        {
            return new MarginTradingAccount
            {
                Id = src.Id,
                TradingConditionId = src.TradingConditionId,
                ClientId = src.ClientId,
                BaseAssetId = src.BaseAssetId,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                AccountFpl = accountFpl ?? new AccountFpl {ActualHash = 1},
                LegalEntity = src.LegalEntity,
                IsDisabled = src.IsDisabled,
                IsDeleted = src.IsDeleted,
                LastUpdateTime = src.LastUpdateTime,
                LastBalanceChangeTime = src.LastBalanceChangeTime,
                IsWithdrawalDisabled = src.IsWithdrawalDisabled
            };
        }

        public int CompareTo(MarginTradingAccount other)
        {
            var result = string.Compare(Id, other.Id, StringComparison.Ordinal);
            return 0 != result ? result : string.Compare(ClientId, other.ClientId, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() ^ ClientId.GetHashCode();
        }
    }

    public enum AccountLevel
    {
        None = 0,
        MarginCall1 = 1,
        MarginCall2 = 2,
        OvernightMarginCall = 3,
        StopOut = 4,
    }

    public static class MarginTradingAccountExtensions
    {
        private static AccountFpl GetAccountFpl(this IMarginTradingAccount account)
        {
            if (account is MarginTradingAccount accountInstance)
            {
                if (accountInstance.AccountFpl.ActualHash != accountInstance.AccountFpl.CalculatedHash)
                {
                    MtServiceLocator.AccountUpdateService.UpdateAccount(account);
                }

                return accountInstance.AccountFpl;
            }

            return new AccountFpl();
        }

        public static AccountLevel GetAccountLevel(this IMarginTradingAccount account,
            decimal? overnightUsedMargin = null)
        {
            var marginUsageLevel = account.GetMarginUsageLevel(overnightUsedMargin);
            var accountFplData = account.GetAccountFpl();

            #region Account Level
            
            if (marginUsageLevel <= accountFplData.StopOutLevel)
                return AccountLevel.StopOut;

            if (marginUsageLevel <= accountFplData.MarginCall2Level)
                return AccountLevel.MarginCall2;
            
            if (marginUsageLevel <= accountFplData.MarginCall1Level)
                return AccountLevel.MarginCall1;
            
            #endregion

            if (MtServiceLocator.McoRules == null)
            {
                return AccountLevel.None;
            }
            
            #region MCO long level

            if (accountFplData.McoMarginUsageLevelLong != 0)
            {
                if (accountFplData.McoMarginUsageLevelLong <= MtServiceLocator.McoRules.LongMcoLevels.StopOut)
                    return AccountLevel.StopOut;
            
                if (accountFplData.McoMarginUsageLevelLong <= MtServiceLocator.McoRules.LongMcoLevels.MarginCall2)
                    return AccountLevel.MarginCall2;
            
                if (accountFplData.McoMarginUsageLevelLong <= MtServiceLocator.McoRules.LongMcoLevels.MarginCall1)
                    return AccountLevel.MarginCall1;
            }
            
            #endregion
            
            #region MCO short level

            if (accountFplData.McoMarginUsageLevelShort != 0)
            {
                if (accountFplData.McoMarginUsageLevelShort >= MtServiceLocator.McoRules.ShortMcoLevels.StopOut)
                    return AccountLevel.StopOut;
            
                if (accountFplData.McoMarginUsageLevelShort >= MtServiceLocator.McoRules.ShortMcoLevels.MarginCall2)
                    return AccountLevel.MarginCall2;
            
                if (accountFplData.McoMarginUsageLevelShort >= MtServiceLocator.McoRules.ShortMcoLevels.MarginCall1)
                    return AccountLevel.MarginCall1;
            }
            
            #endregion
            
            return AccountLevel.None;
        }

        public static decimal GetMarginUsageLevel(this IMarginTradingAccount account,
            decimal? overnightUsedMargin = null)
        {
            var totalCapital = account.GetTotalCapital();
            
            var usedMargin = overnightUsedMargin ?? account.GetUsedMargin();

            //Anton Belkin said 100 is ok )
            if (usedMargin <= 0)
                return 100;

            return totalCapital / usedMargin;
        }

        public static decimal GetTotalCapital(this IMarginTradingAccount account)
        {
            return account.Balance + account.GetUnrealizedDailyPnl() - account.GetFrozenMargin() +
                   account.GetUnconfirmedMargin();
        }

        public static decimal GetPnl(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().PnL;
        }

        public static decimal GetUnrealizedDailyPnl(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().UnrealizedDailyPnl;
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

        public static decimal GetFrozenMargin(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().WithdrawalFrozenMargin;
        }

        public static decimal GetUnconfirmedMargin(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().UnconfirmedMargin;
        }

        public static decimal GetMarginCall1Level(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().MarginCall1Level;
        }
        
        public static decimal GetMarginCall2Level(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().MarginCall2Level;
        }

        public static decimal GetStopOutLevel(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().StopOutLevel;
        }
        
        public static decimal GetMcoMarginUsageLevelLong(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().McoMarginUsageLevelLong;
        }
        
        public static decimal GetMcoMarginUsageLevelShort(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().McoMarginUsageLevelShort;
        }

        public static int GetOpenPositionsCount(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().OpenPositionsCount;
        }

        public static int GetActiveOrdersCount(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().ActiveOrdersCount;
        }
        
        public static void CacheNeedsToBeUpdated(this IMarginTradingAccount account)
        {
            if (account is MarginTradingAccount accountInstance)
            {
                accountInstance.AccountFpl.ActualHash++;
            }
        }
        
        public static bool IsInLiquidation(this IMarginTradingAccount account)
        {
            return !string.IsNullOrEmpty(account.LiquidationOperationId);
        }
    }
}

