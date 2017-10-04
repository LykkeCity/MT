using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarginTrading.Core
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
                Type = src.Type
            };
        }
    }

    public enum AccountHistoryType
    {
        Deposit,
        Withdraw,
        OrderClosed,
        Reset
    }

    public interface IMarginTradingAccountHistoryRepository
    {
        Task AddAsync(IMarginTradingAccountHistory accountHistory);
        Task<IEnumerable<IMarginTradingAccountHistory>> GetAsync(string[] accountIds, DateTime? from, DateTime? to);
    }
}
