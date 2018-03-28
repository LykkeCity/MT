using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Backend.Core
{
    public interface IMarginTradingAccountHistory
    {
        string Id { get; }
        DateTime Date { get; }
        string AccountId { get; }
        string ClientId { get; }
        decimal Amount { get; }
        decimal Balance { get; }
        decimal WithdrawTransferLimit { get; }
        string Comment { get; }
        AccountHistoryType Type { get; }
        string OrderId { get; }
        string LegalEntity { get; }
        string AuditLog { get; }
    }

    public class MarginTradingAccountHistory : IMarginTradingAccountHistory
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string AccountId { get; set; }
        public string ClientId { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public decimal WithdrawTransferLimit { get; set; }
        public string Comment { get; set; }
        public AccountHistoryType Type { get; set; }
        public string OrderId { get; set; }
        public string LegalEntity { get; set; }
        public string AuditLog { get; set; }


        public static MarginTradingAccountHistory Create(IMarginTradingAccountHistory src)
        {
            return new MarginTradingAccountHistory
            {
                Id = src.Id,
                AccountId = src.AccountId,
                Date = src.Date,
                ClientId = src.ClientId,
                Amount = src.Amount,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                Comment = src.Comment,
                Type = src.Type,
                OrderId = src.OrderId,
                LegalEntity = src.LegalEntity,
                AuditLog = src.AuditLog
            };
        }
    }

    public enum AccountHistoryType
    {
        Deposit,
        Withdraw,
        OrderClosed,
        Reset,
        Swap,
        Manual
    }

    public interface IMarginTradingAccountHistoryRepository
    {
        Task AddAsync(IMarginTradingAccountHistory accountHistory);
        Task<IReadOnlyList<IMarginTradingAccountHistory>> GetAsync(string[] accountIds, DateTime? from, DateTime? to);
    }
}
