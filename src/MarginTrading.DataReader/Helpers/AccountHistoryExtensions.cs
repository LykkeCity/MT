using MarginTrading.Backend.Contracts.AccountHistory;
using MarginTrading.Backend.Core;
using MarginTrading.Common.Extensions;

namespace MarginTrading.DataReader.Helpers
{
    public static class AccountHistoryExtensions
    {
        public static AccountHistoryContract ToBackendContract(this IMarginTradingAccountHistory src)
        {
            return new AccountHistoryContract
            {
                Id = src.Id,
                Date = src.Date,
                AccountId = src.AccountId,
                ClientId = src.ClientId,
                Amount = src.Amount,
                Balance = src.Balance,
                WithdrawTransferLimit = src.WithdrawTransferLimit,
                Comment = src.Comment,
                Type = src.Type.ToType<AccountHistoryTypeContract>(),
                OrderId = src.OrderId,
                LegalEntity = src.LegalEntity,
                AuditLog = src.AuditLog
            };
        }
    }
}
