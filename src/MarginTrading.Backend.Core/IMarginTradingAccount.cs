// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Common.Log;
using JetBrains.Annotations;
using MarginTrading.Backend.Core.Services;
using MarginTrading.Common.Services;

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
        string AdditionalInfo { get; }
        string AccountName { get; }
        decimal TodayRealizedPnL { get; }
        decimal TodayUnrealizedPnL { get; }
        decimal TodayDepositAmount { get; }
        decimal TodayWithdrawAmount { get; }
        decimal TodayCommissionAmount { get; }
        decimal TodayOtherAmount { get; }
        decimal TodayStartBalance { get; }
        string LogInfo { get; set; }
        decimal TemporaryCapital { get; set; }
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
        public string AdditionalInfo { get; set; }
        public string AccountName { get; set; }
        public decimal TodayRealizedPnL { get; set; }
        public decimal TodayUnrealizedPnL { get; set; }
        public decimal TodayDepositAmount { get; set; }
        public decimal TodayWithdrawAmount { get; set; }
        public decimal TodayCommissionAmount { get; set; }
        public decimal TodayOtherAmount { get; set; }
        public decimal TodayStartBalance { get; set; }
        public string LogInfo { get; set; }
        public AccountFpl AccountFpl { get; private set; } = new AccountFpl();
        public decimal TemporaryCapital { get; set; }
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
                IsWithdrawalDisabled = src.IsWithdrawalDisabled,
                AdditionalInfo = src.AdditionalInfo,
                AccountName = src.AccountName,
                TemporaryCapital = src.TemporaryCapital,
            };
        }

        public string Reset(DateTime eventTime)
        {
            var warnings = new List<string>();

            if (AccountFpl.UnconfirmedMarginData.Any())
            {
                warnings.Add($"There is some unconfirmed margin data on account: {string.Join(",", AccountFpl.UnconfirmedMarginData)}. ");
            }

            if (AccountFpl.WithdrawalFrozenMarginData.Any())
            {
                warnings.Add($"There is some withdrawal frozen margin data on account: {string.Join(",", AccountFpl.WithdrawalFrozenMarginData)}. ");
            }
            
            Balance = 0;
            TodayStartBalance = 0;
            TodayRealizedPnL = 0;
            TodayUnrealizedPnL = 0;
            TodayDepositAmount = 0;
            TodayWithdrawAmount = 0;
            TodayCommissionAmount = 0;
            TodayOtherAmount = 0;
            LastUpdateTime = LastBalanceChangeTime = eventTime;
            AccountFpl = new AccountFpl();
            TemporaryCapital = 0;

            return string.Join(", ", warnings);
        }

        public bool TryFreezeWithdrawalMargin(string operationId, decimal amount)
        {
            if (string.IsNullOrWhiteSpace(operationId))
                throw new ArgumentNullException(nameof(operationId));
            
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to withdraw must be positive");
            
            if (AccountFpl.WithdrawalFrozenMarginData.TryAdd(operationId, amount))
            {
                AccountFpl.WithdrawalFrozenMargin = AccountFpl.WithdrawalFrozenMarginData.Values.Sum();
                return true;
            }
            
            return false;
        }
        
        public bool TryUnfreezeWithdrawalMargin(string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId))
                throw new ArgumentNullException(nameof(operationId));
            
            if (AccountFpl.WithdrawalFrozenMarginData.TryRemove(operationId, out _))
            {
                AccountFpl.WithdrawalFrozenMargin = AccountFpl.WithdrawalFrozenMarginData.Values.Sum();
                return true;
            }
            
            return false;
        }
        
        public bool CanWithdraw(decimal disposableCapital, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount to withdraw must be positive");
            
            return disposableCapital >= amount;
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
                if (accountInstance.AccountFpl.ActualHash == accountInstance.AccountFpl.CalculatedHash)
                    return accountInstance.AccountFpl;

                try
                {
                    using var scope = ContainerProvider.Container.BeginLifetimeScope();
                    var svc = scope.Resolve<IAccountUpdateService>();
                    svc.UpdateAccount(accountInstance);
                }
                catch (Exception)
                {
                    LogLocator.CommonLog.WriteWarning(nameof(MarginTradingAccountExtensions),
                        $"AccountId = {account.Id}",
                        "Couldn't update account FPL");
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

            if (marginUsageLevel <= accountFplData.StopOutLevel)
                return AccountLevel.StopOut;

            if (marginUsageLevel <= accountFplData.MarginCall2Level)
                return AccountLevel.MarginCall2;
            
            if (marginUsageLevel <= accountFplData.MarginCall1Level)
                return AccountLevel.MarginCall1;
            
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
            var fplData = account.GetAccountFpl();
            //According to ESMA MCO rule a stop-out added at 50% of the initially invested margin
            //So if the 50% of the initially invested margin (accumulated per the account), is bigger than the recalculated 100%, than this margin requirement is the reference for the free capital determination and the liquidation level.
            return Math.Max(fplData.UsedMargin, Math.Round(fplData.InitiallyUsedMargin/2, AssetsConstants.DefaultAssetAccuracy));
        }
        
        public static decimal GetCurrentlyUsedMargin(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().UsedMargin;
        }
        
        public static decimal GetInitiallyUsedMargin(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().InitiallyUsedMargin;
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
            return account.GetTotalCapital() - Math.Max(account.GetMarginInit(), account.GetUsedMargin());
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
        
        public static int GetOpenPositionsCount(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().OpenPositionsCount;
        }

        public static int GetActiveOrdersCount(this IMarginTradingAccount account)
        {
            return account.GetAccountFpl().ActiveOrdersCount;
        }
        
        public static void CacheNeedsToBeUpdated(this MarginTradingAccount account)
        {
            account.AccountFpl.ActualHash++;
        }
    }
}

